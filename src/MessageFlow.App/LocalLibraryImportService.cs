using System.IO.Compression;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using MessageFlow.Core.ContentSources;
using MessageFlow.Core.Sermons;
using MessageFlow.Core.Songs;
using MessageFlow.Data;
using MessageFlow.Importer;
using MessageFlow.Search;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.App;

public sealed class LocalLibraryImportService
{
    public const string SermonType = "Brother Frank Publications";
    public const string SongType = "Additional Songs";
    private const string FrankSourceName = "brother_frank_custom";
    private readonly MessageFlowDbContext dbContext;
    private readonly string managedStorageRoot;

    public LocalLibraryImportService(MessageFlowDbContext dbContext, string? managedStorageRoot = null)
    {
        this.dbContext = dbContext;
        this.managedStorageRoot = managedStorageRoot ?? Path.Combine(
            Path.GetDirectoryName(MessageFlowDatabase.DefaultDatabasePath)!,
            "custom-library");
    }

    public async Task<LibraryImportCandidate> ScanAsync(
        string filePath,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(filePath);
        var hash = await ComputeSha256Async(fullPath, cancellationToken);
        var managedPath = GetManagedPath(contentType, fullPath, hash);

        if (contentType == SermonType)
        {
            return await ScanSermonAsync(fullPath, hash, managedPath, cancellationToken);
        }

        return await ScanSongAsync(fullPath, hash, managedPath, cancellationToken);
    }

    public async Task<int> ImportAsync(
        IReadOnlyCollection<LibraryImportCandidate> candidates,
        CancellationToken cancellationToken = default)
    {
        var selected = candidates.Where(candidate => candidate.IsSelected && candidate.CanImport).ToList();
        var imported = 0;

        foreach (var candidate in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var managedPath = GetManagedPath(candidate.ContentType, candidate.SourcePath, candidate.Sha256);
            Directory.CreateDirectory(Path.GetDirectoryName(managedPath)!);
            var createdManagedCopy = false;

            try
            {
                if (!File.Exists(managedPath))
                {
                    File.Copy(candidate.SourcePath, managedPath, overwrite: false);
                    createdManagedCopy = true;
                }

                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                if (candidate.ContentType == SermonType)
                {
                    await ImportSermonAsync(candidate, managedPath, cancellationToken);
                }
                else
                {
                    await ImportSongAsync(candidate, managedPath, cancellationToken);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                candidate.Status = $"Imported to managed library at {DateTime.Now:g}.";
                candidate.IsSelected = false;
                imported++;
            }
            catch
            {
                dbContext.ChangeTracker.Clear();
                if (createdManagedCopy && File.Exists(managedPath))
                {
                    File.Delete(managedPath);
                }

                throw;
            }
        }

        return imported;
    }

    private async Task<LibraryImportCandidate> ScanSermonAsync(
        string filePath,
        string hash,
        string managedPath,
        CancellationToken cancellationToken)
    {
        if (!Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Unsupported(filePath, SermonType, hash, "Unsupported: Brother Frank import currently accepts text-based PDF files only.");
        }

        if (await dbContext.Sermons.AsNoTracking().AnyAsync(sermon => sermon.SourceFilePath == managedPath, cancellationToken))
        {
            return Unsupported(filePath, SermonType, hash, "Duplicate SHA-256: this file is already in the managed Sermon library.");
        }

        try
        {
            var pages = new PdfTextExtractor().ExtractPages(filePath);
            var characterCount = pages.Sum(page => page.Text.Count(character => !char.IsWhiteSpace(character)));
            if (characterCount < 40)
            {
                return Unsupported(filePath, SermonType, hash, "Unsupported / Needs Review: no reliable embedded PDF text was found. OCR is not performed.");
            }

            var paragraphs = ParagraphSplitter.Split(pages);
            if (paragraphs.Count == 0)
            {
                return Unsupported(filePath, SermonType, hash, "Unsupported / Needs Review: embedded text produced no paragraphs.");
            }

            var sourceContext = new SourceMetadataContext(0, FrankSourceName, "Brother Frank Publications", "Book");
            var metadata = SermonMetadataParser.Parse(filePath, Path.GetDirectoryName(filePath)!, sourceContext);
            var title = string.IsNullOrWhiteSpace(metadata.Title)
                ? Path.GetFileNameWithoutExtension(filePath)
                : metadata.Title;
            var conflict = await dbContext.Sermons.AsNoTracking().AnyAsync(
                sermon => sermon.Title == title && sermon.SermonCode == metadata.SermonCode,
                cancellationToken);
            var status = conflict
                ? "Needs Review: title/code conflict detected. Correct the title or confirm selection before import."
                : $"Ready: {paragraphs.Count:N0} paragraphs with embedded text.";

            return new LibraryImportCandidate(filePath, SermonType, title, hash, status, true, paragraphs.Count)
            {
                IsSelected = !conflict,
                PreparedContent = new PreparedSermon(metadata, paragraphs)
            };
        }
        catch (Exception ex)
        {
            return Unsupported(filePath, SermonType, hash, $"Unsupported / Needs Review: PDF text extraction failed. {ex.Message}");
        }
    }

    private async Task<LibraryImportCandidate> ScanSongAsync(
        string filePath,
        string hash,
        string managedPath,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Songs.AsNoTracking().AnyAsync(
                song => song.SourceFilePath == managedPath || song.ContentHash == hash,
                cancellationToken))
        {
            return Unsupported(filePath, SongType, hash, "Duplicate SHA-256: this file is already in the Song library.");
        }

        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            List<PreparedSongSection> sections = extension switch
            {
                ".pptx" => ExtractPptxSections(filePath),
                ".txt" => await ExtractTxtSectionsAsync(filePath, cancellationToken),
                ".pdf" => ExtractPdfSongSections(filePath),
                ".ppt" => ExtractLegacyPptSections(filePath),
                _ => []
            };

            if (sections.Count == 0)
            {
                return Unsupported(filePath, SongType, hash, "Unsupported / Needs Review: no reliable song text was found.");
            }

            var title = DetectSongTitle(filePath, sections);
            var normalizedTitle = SongTextNormalizer.Normalize(title);
            var conflict = await dbContext.Songs.AsNoTracking().AnyAsync(
                song => song.NormalizedTitle == normalizedTitle,
                cancellationToken);
            var status = conflict
                ? "Needs Review: an existing Song has the same normalized title. Correct the title or confirm selection."
                : $"Ready: {sections.Count:N0} ordered sections.";

            return new LibraryImportCandidate(filePath, SongType, title, hash, status, true, sections.Count)
            {
                IsSelected = !conflict,
                PreparedContent = sections
            };
        }
        catch (Exception ex)
        {
            return Unsupported(filePath, SongType, hash, $"Unsupported / Needs Review: text extraction failed. {ex.Message}");
        }
    }

    private async Task ImportSermonAsync(
        LibraryImportCandidate candidate,
        string managedPath,
        CancellationToken cancellationToken)
    {
        if (candidate.PreparedContent is not PreparedSermon prepared)
        {
            throw new InvalidOperationException("The Sermon must be scanned again before import.");
        }

        var source = await dbContext.ContentSources.FirstOrDefaultAsync(item => item.Name == FrankSourceName, cancellationToken);
        if (source is null)
        {
            source = new ContentSource
            {
                Name = FrankSourceName,
                DisplayName = "Brother Frank Publications",
                SourceType = "Book",
                Description = "Locally imported Brother Frank publications stored in MessageFlow-managed storage.",
                LocalFolderPath = Path.GetDirectoryName(managedPath),
                CreatedAt = DateTime.UtcNow
            };
            dbContext.ContentSources.Add(source);
        }
        else
        {
            source.DisplayName = "Brother Frank Publications";
            source.SourceType = "Book";
            source.Description = "Locally imported Brother Frank publications stored in MessageFlow-managed storage.";
            source.LocalFolderPath = Path.GetDirectoryName(managedPath);
        }

        var author = await dbContext.Authors.FirstOrDefaultAsync(item => item.DisplayName == "Brother Frank", cancellationToken);
        if (author is null)
        {
            author = new Author
            {
                FullName = "Ewald Frank",
                DisplayName = "Brother Frank",
                Description = "Brother Frank local custom publication library."
            };
            dbContext.Authors.Add(author);
        }

        dbContext.Sermons.Add(new Sermon
        {
            Author = author,
            ContentSource = source,
            Title = candidate.Title.Trim(),
            SermonCode = prepared.Metadata.SermonCode,
            Year = prepared.Metadata.Year,
            Date = prepared.Metadata.Date,
            Location = prepared.Metadata.Location,
            Language = prepared.Metadata.Language,
            SourceFilePath = managedPath,
            CreatedAt = DateTime.UtcNow,
            Paragraphs = prepared.Paragraphs.Select(paragraph => new SermonParagraph
            {
                ParagraphNumber = paragraph.ParagraphNumber,
                Text = paragraph.Text,
                SearchText = paragraph.SearchText,
                PageNumber = paragraph.PageNumber,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        });
        dbContext.ImportLogs.Add(new ImportLog
        {
            FilePath = managedPath,
            Status = "Imported",
            Message = $"Brother Frank local import. Original filename: {candidate.FileName}. SHA-256: {candidate.Sha256}. Paragraphs: {prepared.Paragraphs.Count}.",
            ImportedAt = DateTime.UtcNow
        });
    }

    private Task ImportSongAsync(
        LibraryImportCandidate candidate,
        string managedPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (candidate.PreparedContent is not IReadOnlyList<PreparedSongSection> sections)
        {
            throw new InvalidOperationException("The Song must be scanned again before import.");
        }

        dbContext.Songs.Add(new Song
        {
            Title = candidate.Title.Trim(),
            NormalizedTitle = SongTextNormalizer.Normalize(candidate.Title),
            SourceFilePath = managedPath,
            SourceFolder = "Custom Song",
            FileName = candidate.FileName,
            ImportedAtUtc = DateTime.UtcNow,
            ContentHash = candidate.Sha256,
            WarningSummary = string.Empty,
            Language = "en",
            IsActive = true,
            Sections = sections.Select((section, index) => new SongSection
            {
                SectionOrder = index + 1,
                SectionType = section.Type,
                SectionLabel = section.Label,
                Text = section.Text,
                NormalizedText = SongTextNormalizer.Normalize(section.Text)
            }).ToList()
        });
        return Task.CompletedTask;
    }

    private static List<PreparedSongSection> ExtractPptxSections(string filePath)
    {
        using var archive = ZipFile.OpenRead(filePath);
        XNamespace drawing = "http://schemas.openxmlformats.org/drawingml/2006/main";
        return archive.Entries
            .Where(entry => entry.FullName.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase) &&
                            entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => ExtractNumber(entry.FullName))
            .Select((entry, index) =>
            {
                using var stream = entry.Open();
                var document = XDocument.Load(stream);
                var lines = document.Descendants(drawing + "p")
                    .Select(paragraph => string.Concat(paragraph.Descendants(drawing + "t").Select(text => text.Value)))
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();
                return new PreparedSongSection("Slide", $"Slide {index + 1}", string.Join(Environment.NewLine, lines));
            })
            .Where(section => !string.IsNullOrWhiteSpace(section.Text))
            .ToList();
    }

    private static async Task<List<PreparedSongSection>> ExtractTxtSectionsAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(filePath, cancellationToken);
        text = text.TrimStart('\uFEFF').Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return System.Text.RegularExpressions.Regex.Split(text, @"\n[ \t]*\n+")
            .Select((section, index) => new PreparedSongSection(
                "Section",
                $"Section {index + 1}",
                section.Trim('\n')))
            .Where(section => !string.IsNullOrWhiteSpace(section.Text))
            .ToList();
    }

    private static List<PreparedSongSection> ExtractLegacyPptSections(string filePath)
    {
        var appType = Type.GetTypeFromProgID("PowerPoint.Application")
            ?? throw new NotSupportedException("Legacy PPT needs Microsoft PowerPoint on this computer. Convert to PPTX when PowerPoint is unavailable.");
        object? application = null;
        object? presentation = null;
        try
        {
            dynamic powerPoint = Activator.CreateInstance(appType)!;
            application = powerPoint;
            powerPoint.DisplayAlerts = 0;
            dynamic presentations = powerPoint.Presentations;
            presentation = presentations.Open(filePath, -1, 0, 0);
            dynamic deck = presentation;
            var sections = new List<PreparedSongSection>();
            foreach (dynamic slide in deck.Slides)
            {
                var lines = new List<string>();
                foreach (dynamic shape in slide.Shapes)
                {
                    ExtractComShapeText(shape, lines);
                }

                if (lines.Count > 0)
                {
                    sections.Add(new PreparedSongSection("Slide", $"Slide {sections.Count + 1}", string.Join(Environment.NewLine, lines)));
                }
            }

            deck.Close();
            presentation = null;
            powerPoint.Quit();
            application = null;
            return sections;
        }
        finally
        {
            TryCloseComObject(presentation, closePresentation: true);
            TryCloseComObject(application, closePresentation: false);
        }
    }

    private static void ExtractComShapeText(dynamic shape, ICollection<string> lines)
    {
        try
        {
            if (Convert.ToInt32(shape.HasTextFrame, CultureInfo.InvariantCulture) != 0 &&
                Convert.ToInt32(shape.TextFrame.HasText, CultureInfo.InvariantCulture) != 0)
            {
                var text = Convert.ToString(shape.TextFrame.TextRange.Text, CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(text))
                {
                    foreach (var line in text.Split(new[] { "\r\n", "\n", "\r", "\v" }, StringSplitOptions.None))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            lines.Add(line);
                        }
                    }
                }
            }
        }
        catch
        {
            // Some PowerPoint shapes do not expose a readable text frame.
        }

        try
        {
            foreach (dynamic child in shape.GroupItems)
            {
                ExtractComShapeText(child, lines);
            }
        }
        catch
        {
            // The shape is not a group.
        }
    }

    private static void TryCloseComObject(object? value, bool closePresentation)
    {
        if (value is null)
        {
            return;
        }

        try
        {
            dynamic instance = value;
            if (closePresentation)
            {
                instance.Close();
            }
            else
            {
                instance.Quit();
            }
        }
        catch
        {
            // Best-effort COM cleanup.
        }
        finally
        {
            try
            {
                if (Marshal.IsComObject(value))
                {
                    Marshal.FinalReleaseComObject(value);
                }
            }
            catch
            {
                // Best-effort COM cleanup.
            }
        }
    }

    private static List<PreparedSongSection> ExtractPdfSongSections(string filePath)
    {
        return new PdfTextExtractor().ExtractPages(filePath)
            .Where(page => !string.IsNullOrWhiteSpace(page.Text))
            .Select((page, index) => new PreparedSongSection("Page", $"Page {index + 1}", page.Text))
            .ToList();
    }

    private static string DetectSongTitle(string filePath, IReadOnlyList<PreparedSongSection> sections)
    {
        var firstLine = sections.Select(section => section.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        return firstLine is { Length: <= 100 }
            ? firstLine.Trim()
            : Path.GetFileNameWithoutExtension(filePath).Replace('_', ' ').Trim();
    }

    private static int ExtractNumber(string value)
    {
        var digits = new string(Path.GetFileNameWithoutExtension(value).Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : int.MaxValue;
    }

    private static LibraryImportCandidate Unsupported(string path, string type, string hash, string status)
        => new(path, type, Path.GetFileNameWithoutExtension(path), hash, status, false, 0);

    private string GetManagedPath(string contentType, string originalPath, string hash)
    {
        var kind = contentType == SermonType ? "publications" : "songs";
        var safeName = string.Concat(Path.GetFileNameWithoutExtension(originalPath)
            .Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        safeName = safeName.Length > 80 ? safeName[..80] : safeName;
        return Path.Combine(
            managedStorageRoot,
            kind,
            $"{hash[..16]}-{safeName}{Path.GetExtension(originalPath).ToLowerInvariant()}");
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private sealed record PreparedSermon(SermonMetadata Metadata, IReadOnlyList<ParagraphDraft> Paragraphs);
    private sealed record PreparedSongSection(string Type, string Label, string Text);
}
