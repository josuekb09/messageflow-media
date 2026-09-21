using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MessageFlow.Core.Sermons;
using MessageFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Importer;

public sealed partial class PdfSermonImporter(MessageFlowDbContext dbContext)
{
    private const int AuthorId = 1;
    private readonly PdfTextExtractor textExtractor = new();

    public async Task<ImportSummary> ImportAsync(ImportOptions options, CancellationToken cancellationToken = default)
    {
        Report(options, "Scanning PDF files...", 0, 0, 0, 0, 0);

        var pdfFiles = Directory.EnumerateFiles(options.SourceRoot, "*.pdf", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var summary = new ImportSummary
        {
            TotalFiles = pdfFiles.Count
        };

        Console.WriteLine($"PDF files found: {summary.TotalFiles}");
        Console.WriteLine();
        Report(options, $"Found {summary.TotalFiles:N0} PDF files.", 0, summary.TotalFiles, 0, 0, 0);

        var sourceContext = await LoadSourceMetadataContextAsync(options, cancellationToken);
        var authorId = await EnsureAuthorExistsAsync(sourceContext, options.SourceRoot, cancellationToken);
        if (options.Reset)
        {
            await ResetImportedSermonsAsync(options.SourceRoot, cancellationToken);
        }

        for (var index = 0; index < pdfFiles.Count; index++)
        {
            var filePath = Path.GetFullPath(pdfFiles[index]);
            Console.WriteLine($"[{index + 1}/{summary.TotalFiles}] {filePath}");
            Report(
                options,
                $"Importing {Path.GetFileName(filePath)}",
                index + 1,
                summary.TotalFiles,
                summary.ImportedParagraphs,
                summary.SkippedFiles,
                summary.ErrorCount);

            try
            {
                var result = await ImportFileAsync(filePath, options, authorId, sourceContext, cancellationToken);

                if (result.Skipped)
                {
                    summary.SkippedFiles++;
                    Console.WriteLine("  skipped: already imported");
                    Report(
                        options,
                        $"Skipped {Path.GetFileName(filePath)}: already imported.",
                        index + 1,
                        summary.TotalFiles,
                        summary.ImportedParagraphs,
                        summary.SkippedFiles,
                        summary.ErrorCount);
                    continue;
                }

                summary.ImportedFiles++;
                summary.ImportedParagraphs += result.ParagraphCount;
                Console.WriteLine($"  imported paragraphs: {result.ParagraphCount}");
                Report(
                    options,
                    $"Imported {Path.GetFileName(filePath)}.",
                    index + 1,
                    summary.TotalFiles,
                    summary.ImportedParagraphs,
                    summary.SkippedFiles,
                    summary.ErrorCount);
            }
            catch (Exception ex)
            {
                summary.ErrorCount++;
                dbContext.ChangeTracker.Clear();
                await WriteImportLogAsync(filePath, "Error", ex.Message, cancellationToken);
                Console.WriteLine($"  error: {ex.Message}");
                Report(
                    options,
                    $"Error importing {Path.GetFileName(filePath)}: {ex.Message}",
                    index + 1,
                    summary.TotalFiles,
                    summary.ImportedParagraphs,
                    summary.SkippedFiles,
                    summary.ErrorCount);
            }
        }

        Report(
            options,
            "Import complete.",
            summary.TotalFiles,
            summary.TotalFiles,
            summary.ImportedParagraphs,
            summary.SkippedFiles,
            summary.ErrorCount);

        return summary;
    }

    private async Task<ImportFileResult> ImportFileAsync(
        string filePath,
        ImportOptions options,
        int authorId,
        SourceMetadataContext? sourceContext,
        CancellationToken cancellationToken)
    {
        var existingSermon = await dbContext.Sermons
            .AsNoTracking()
            .Where(sermon => sermon.SourceFilePath == filePath)
            .Select(sermon => new { sermon.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingSermon is not null && !options.Force)
        {
            await WriteImportLogAsync(filePath, "Skipped", "File already exists in the sermon database.", cancellationToken);
            return ImportFileResult.Skip;
        }

        // Check for browser download copy, e.g. "197106-Circular-english (1).pdf"
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        if (Regex.IsMatch(fileNameWithoutExt, @"\s*\(\d+\)$"))
        {
            var baseName = Regex.Replace(fileNameWithoutExt, @"\s*\(\d+\)$", string.Empty).Trim();
            var dir = Path.GetDirectoryName(filePath);
            var ext = Path.GetExtension(filePath);
            if (dir is not null)
            {
                var canonicalFile = Path.Combine(dir, baseName + ext);
                if (File.Exists(canonicalFile))
                {
                    await WriteImportLogAsync(filePath, "Skipped", $"Duplicate browser copy of {Path.GetFileName(canonicalFile)}", cancellationToken);
                    return ImportFileResult.Skip;
                }
            }
        }

        var pages = textExtractor.ExtractPages(filePath);
        var metadata = SermonMetadataParser.Parse(filePath, options.SourceRoot, sourceContext);
        if (!string.IsNullOrWhiteSpace(options.LanguageOverride) &&
            !string.Equals(metadata.Language, options.LanguageOverride, StringComparison.OrdinalIgnoreCase))
        {
            metadata = metadata with { Language = options.LanguageOverride };
        }

        if (!options.Force)
        {
            // Scoped to the content source on purpose. A global check lets a document in one
            // library block an unrelated one in another: retired test imports held the codes
            // CL-2020-04 and CL-2020-12 and kept the real 2020 circular letters out of the
            // Brother Frank library. It also protects the Branham collection, where two files
            // can legitimately share a date code.
            var contentSourceId = sourceContext?.Id;
            var duplicateByCode = await dbContext.Sermons
                .AsNoTracking()
                .Where(sermon => sermon.AuthorId == authorId &&
                                 sermon.ContentSourceId == contentSourceId &&
                                 sermon.SermonCode == metadata.SermonCode &&
                                 sermon.SourceFilePath != filePath)
                .Select(sermon => new { sermon.Id, sermon.SourceFilePath })
                .FirstOrDefaultAsync(cancellationToken);

            if (duplicateByCode is not null)
            {
                await WriteImportLogAsync(filePath, "Skipped", $"Duplicate publication code {metadata.SermonCode} (already imported from {Path.GetFileName(duplicateByCode.SourceFilePath)}).", cancellationToken);
                return ImportFileResult.Skip;
            }

            // The same publication can reach the library under two filename styles, which produce
            // two different codes, so the code check alone cannot see them as one document.
            // Comparing the file itself catches that.
            var duplicateByContent = await FindDuplicateByContentAsync(
                filePath,
                contentSourceId,
                cancellationToken);

            if (duplicateByContent is not null)
            {
                await WriteImportLogAsync(filePath, "Skipped", $"Identical file already imported as {Path.GetFileName(duplicateByContent)}.", cancellationToken);
                return ImportFileResult.Skip;
            }
        }

        if (string.Equals(metadata.Language, "sw", StringComparison.OrdinalIgnoreCase) &&
            SwahiliPdfTitleExtractor.TryExtractFromPdf(filePath, out var swahiliTitle))
        {
            metadata = metadata with { Title = swahiliTitle };
        }
        else if (string.Equals(metadata.Language, "fr", StringComparison.OrdinalIgnoreCase) &&
                 FrenchPdfTitleExtractor.TryExtractFromPdf(filePath, out var frenchTitle))
        {
            metadata = metadata with { Title = frenchTitle };
        }

        var extractedParagraphs = SermonMetadataParser.IsBrotherBranhamSource(sourceContext)
            ? PdfFirstBranhamBlockExtractor.Split(pages, metadata)
            : ParagraphSplitter.Split(pages);
        var qualitySummary = ParagraphQualitySummary.Empty;
        var paragraphs = extractedParagraphs;
        if (ShouldApplyCircularLetterQualityFilter(sourceContext, metadata))
        {
            var filtered = CircularLetterParagraphQualityFilter.Apply(extractedParagraphs);
            paragraphs = filtered.Paragraphs;
            qualitySummary = filtered.Summary;
        }

        var extractedCharacterCount = pages.Sum(page => page.Text.Length);
        var detectedParagraphNumbers = paragraphs.Count(paragraph => paragraph.HasDetectedParagraphNumber);
        var fallbackParagraphNumbers = paragraphs.Count - detectedParagraphNumbers;
        var preview = TextCleaner.BuildPreview(string.Join(" ", paragraphs.Select(paragraph => paragraph.Text)), 200);

        Console.WriteLine($"  file: {Path.GetFileName(filePath)}");
        Console.WriteLine($"  extracted characters: {extractedCharacterCount:N0}");
        Console.WriteLine($"  extracted paragraph count: {extractedParagraphs.Count:N0}");
        Console.WriteLine($"  imported paragraph count: {paragraphs.Count:N0}");
        Console.WriteLine($"  detected paragraph numbers: {detectedParagraphNumbers:N0}");
        Console.WriteLine($"  fallback paragraph numbers: {fallbackParagraphNumbers:N0}");
        if (qualitySummary.TotalExtractedParagraphs > 0)
        {
            Console.WriteLine(
                "  quality filter: " +
                $"{qualitySummary.AcceptedParagraphs:N0} accepted, " +
                $"{qualitySummary.TotalRejected:N0} rejected " +
                $"(page numbers {qualitySummary.RejectedPageNumbers:N0}, " +
                $"corrupted {qualitySummary.RejectedCorruptedText:N0}, " +
                $"headers/footers {qualitySummary.RejectedHeadersFooters:N0}, " +
                $"too short {qualitySummary.RejectedTooShort:N0})");
        }

        Console.WriteLine($"  preview: {preview}");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (existingSermon is not null)
        {
            dbContext.Sermons.Remove(new Sermon { Id = existingSermon.Id });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var sermon = new Sermon
        {
            AuthorId = authorId,
            ContentSourceId = options.ContentSourceId ?? sourceContext?.Id,
            Title = metadata.Title,
            SermonCode = metadata.SermonCode,
            Year = metadata.Year,
            Date = metadata.Date,
            Location = metadata.Location,
            Language = metadata.Language,
            ContentType = ResolveContentType(metadata, sourceContext),
            SourceFilePath = filePath,
            CreatedAt = DateTime.UtcNow,
            Paragraphs = paragraphs.Select(paragraph => new SermonParagraph
            {
                ParagraphNumber = paragraph.ParagraphNumber,
                Text = paragraph.Text,
                SearchText = paragraph.SearchText,
                PageNumber = paragraph.PageNumber,
                CreatedAt = DateTime.UtcNow
            }).ToList()
        };

        dbContext.Sermons.Add(sermon);
        dbContext.ImportLogs.Add(new ImportLog
        {
            FilePath = filePath,
            Status = existingSermon is null ? "Imported" : "Reimported",
            Message = BuildImportLogMessage(
                paragraphs.Count,
                detectedParagraphNumbers,
                fallbackParagraphNumbers,
                qualitySummary),
            ImportedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ImportFileResult(false, paragraphs.Count);
    }

    private async Task ResetImportedSermonsAsync(string sourceRoot, CancellationToken cancellationToken)
    {
        Console.WriteLine("Reset requested: clearing imported sermons and paragraphs.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var paragraphCount = await dbContext.SermonParagraphs.ExecuteDeleteAsync(cancellationToken);
        var sermonCount = await dbContext.Sermons.ExecuteDeleteAsync(cancellationToken);

        dbContext.ImportLogs.Add(new ImportLog
        {
            FilePath = TrimTo(sourceRoot, 1024),
            Status = "Reset",
            Message = $"Cleared {sermonCount} sermons and {paragraphCount} paragraphs before reimport.",
            ImportedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        Console.WriteLine($"  cleared sermons: {sermonCount:N0}");
        Console.WriteLine($"  cleared paragraphs: {paragraphCount:N0}");
        Console.WriteLine();
    }

    /// <summary>
    /// Finds a document in the same library that was imported from a byte-identical file, and
    /// returns its source path. Returns null when the file has not been imported before.
    /// <para>
    /// Hashing is limited to files whose size already matches an imported one, so an import run
    /// does not read every PDF in the library twice.
    /// </para>
    /// </summary>
    private async Task<string?> FindDuplicateByContentAsync(
        string filePath,
        int? contentSourceId,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            return null;
        }

        var candidates = await dbContext.Sermons
            .AsNoTracking()
            .Where(sermon => sermon.ContentSourceId == contentSourceId &&
                             sermon.SourceFilePath != filePath)
            .Select(sermon => sermon.SourceFilePath)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return null;
        }

        string? incomingHash = null;
        foreach (var candidate in candidates)
        {
            var candidateInfo = new FileInfo(candidate);
            if (!candidateInfo.Exists || candidateInfo.Length != fileInfo.Length)
            {
                continue;
            }

            incomingHash ??= await ComputeFileHashAsync(filePath, cancellationToken);
            var candidateHash = await ComputeFileHashAsync(candidate, cancellationToken);
            if (string.Equals(incomingHash, candidateHash, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Decides what kind of document this is, so one library can hold several types.
    /// Mirrors the classification the database repair applies to existing rows: the publication
    /// code carries the type, and anything unrecognised inherits the source's declared type.
    /// </summary>
    private static string? ResolveContentType(
        SermonMetadata metadata,
        SourceMetadataContext? sourceContext)
    {
        if (SermonMetadataParser.IsBrotherBranhamSource(sourceContext))
        {
            return "Sermon";
        }

        if (metadata.SermonCode.StartsWith("CL-", StringComparison.OrdinalIgnoreCase))
        {
            return "CircularLetter";
        }

        if (EwaldFrankMeetingCodeRegex().IsMatch(metadata.SermonCode))
        {
            return "Meeting";
        }

        if (metadata.SermonCode.StartsWith("EF-", StringComparison.OrdinalIgnoreCase))
        {
            return "Book";
        }

        return string.IsNullOrWhiteSpace(sourceContext?.SourceType) ? null : sourceContext.SourceType;
    }

    /// <summary>Matches a dated meeting code such as EF-1987-01-28-KREFELD.</summary>
    [GeneratedRegex(@"^EF-\d{4}-\d{2}-\d{2}-", RegexOptions.IgnoreCase)]
    private static partial Regex EwaldFrankMeetingCodeRegex();

    private static bool ShouldApplyCircularLetterQualityFilter(
        SourceMetadataContext? sourceContext,
        SermonMetadata metadata)
    {
        if (!SermonMetadataParser.IsEwaldFrankSource(sourceContext))
        {
            return false;
        }

        return string.Equals(sourceContext?.SourceType, "CircularLetter", StringComparison.OrdinalIgnoreCase) ||
               metadata.Title.StartsWith("Circular Letter", StringComparison.OrdinalIgnoreCase) ||
               metadata.SermonCode.StartsWith("CL-", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildImportLogMessage(
        int paragraphCount,
        int detectedParagraphNumbers,
        int fallbackParagraphNumbers,
        ParagraphQualitySummary qualitySummary)
    {
        var message =
            $"Imported {paragraphCount} paragraphs. Detected numbers: {detectedParagraphNumbers}. Fallback numbers: {fallbackParagraphNumbers}.";

        if (qualitySummary.TotalExtractedParagraphs == 0)
        {
            return message;
        }

        return message +
               $" Quality filter: {qualitySummary.AcceptedParagraphs} accepted, {qualitySummary.TotalRejected} rejected " +
               $"({qualitySummary.RejectedPageNumbers} page numbers, {qualitySummary.RejectedCorruptedText} corrupted, " +
               $"{qualitySummary.RejectedHeadersFooters} headers/footers, {qualitySummary.RejectedTooShort} too short).";
    }

    private async Task<SourceMetadataContext?> LoadSourceMetadataContextAsync(
        ImportOptions options,
        CancellationToken cancellationToken)
    {
        if (options.ContentSourceId is null)
        {
            var isFrank = SermonMetadataParser.IsEwaldFrankFilePath(options.SourceRoot) ||
                          options.SourceRoot.Contains("frank", StringComparison.OrdinalIgnoreCase);
            var targetSourceName = isFrank ? "brother_frank" : "brother_branham";

            var defaultSource = await dbContext.ContentSources
                .AsNoTracking()
                .Where(contentSource => contentSource.Name == targetSourceName)
                .Select(contentSource => new
                {
                    contentSource.Id,
                    contentSource.Name,
                    contentSource.DisplayName,
                    contentSource.SourceType
                })
                .FirstOrDefaultAsync(cancellationToken);

            return defaultSource is null
                ? null
                : new SourceMetadataContext(
                    defaultSource.Id,
                    defaultSource.Name,
                    defaultSource.DisplayName,
                    defaultSource.SourceType);
        }

        var source = await dbContext.ContentSources
            .AsNoTracking()
            .Where(contentSource => contentSource.Id == options.ContentSourceId.Value)
            .Select(contentSource => new
            {
                contentSource.Id,
                contentSource.Name,
                contentSource.DisplayName,
                contentSource.SourceType
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (source is null)
        {
            throw new InvalidOperationException($"Content source {options.ContentSourceId.Value} could not be found.");
        }

        return new SourceMetadataContext(
            source.Id,
            source.Name,
            source.DisplayName,
            source.SourceType);
    }

    private async Task<int> EnsureAuthorExistsAsync(
        SourceMetadataContext? sourceContext,
        string sourceRoot,
        CancellationToken cancellationToken)
    {
        // The folder being imported is checked before falling back to Branham. Without it, a
        // source row that could not be resolved makes IsBrotherBranhamSource(null) true, and
        // every Brother Frank PDF in the run would be filed under William Marrion Branham.
        var isEwaldFrank = SermonMetadataParser.IsEwaldFrankSource(sourceContext) ||
                           SermonMetadataParser.IsEwaldFrankFilePath(sourceRoot);

        if (!isEwaldFrank && SermonMetadataParser.IsBrotherBranhamSource(sourceContext))
        {
            return await EnsureBrotherBranhamAuthorExistsAsync(cancellationToken);
        }

        var authorMetadata = SermonMetadataParser.GetAuthorMetadata(sourceContext, sourceRoot);
        var existingAuthor = await dbContext.Authors
            .FirstOrDefaultAsync(author => author.FullName == authorMetadata.FullName, cancellationToken) ??
                             await dbContext.Authors.FirstOrDefaultAsync(
                                 author => author.DisplayName == authorMetadata.DisplayName,
                                 cancellationToken);

        if (existingAuthor is not null)
        {
            var changed = false;
            if (!string.Equals(existingAuthor.FullName, authorMetadata.FullName, StringComparison.Ordinal))
            {
                existingAuthor.FullName = TrimTo(authorMetadata.FullName, 200);
                changed = true;
            }

            if (!string.Equals(existingAuthor.DisplayName, authorMetadata.DisplayName, StringComparison.Ordinal))
            {
                existingAuthor.DisplayName = TrimTo(authorMetadata.DisplayName, 120);
                changed = true;
            }

            if (!string.Equals(existingAuthor.Description, authorMetadata.Description, StringComparison.Ordinal))
            {
                existingAuthor.Description = TrimTo(authorMetadata.Description, 1000);
                changed = true;
            }

            if (changed)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return existingAuthor.Id;
        }

        var author = new Author
        {
            FullName = TrimTo(authorMetadata.FullName, 200),
            DisplayName = TrimTo(authorMetadata.DisplayName, 120),
            Description = TrimTo(authorMetadata.Description, 1000)
        };

        dbContext.Authors.Add(author);
        await dbContext.SaveChangesAsync(cancellationToken);

        return author.Id;
    }

    private async Task<int> EnsureBrotherBranhamAuthorExistsAsync(CancellationToken cancellationToken)
    {
        var exists = await dbContext.Authors.AnyAsync(author => author.Id == AuthorId, cancellationToken);
        if (exists)
        {
            return AuthorId;
        }

        dbContext.Authors.Add(new Author
        {
            Id = AuthorId,
            FullName = "William Marrion Branham",
            DisplayName = "Brother Branham",
            Description = "Primary sermon author for the local MessageFlow sermon library."
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return AuthorId;
    }

    private async Task WriteImportLogAsync(
        string filePath,
        string status,
        string message,
        CancellationToken cancellationToken)
    {
        dbContext.ImportLogs.Add(new ImportLog
        {
            FilePath = TrimTo(filePath, 1024),
            Status = TrimTo(status, 40),
            Message = TrimTo(message, 2000),
            ImportedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string TrimTo(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static void Report(
        ImportOptions options,
        string message,
        int currentFile,
        int totalFiles,
        int importedParagraphs,
        int skippedFiles,
        int errorCount)
    {
        options.Progress?.Report(new ImportProgress(
            message,
            currentFile,
            totalFiles,
            importedParagraphs,
            skippedFiles,
            errorCount));
    }

    private sealed record ImportFileResult(bool Skipped, int ParagraphCount)
    {
        public static ImportFileResult Skip { get; } = new(true, 0);
    }
}
