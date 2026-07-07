using MessageFlow.Core.Sermons;
using MessageFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Importer;

public sealed class PdfSermonImporter(MessageFlowDbContext dbContext)
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
        var authorId = await EnsureAuthorExistsAsync(sourceContext, cancellationToken);
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

        var pages = textExtractor.ExtractPages(filePath);
        var metadata = SermonMetadataParser.Parse(filePath, options.SourceRoot, sourceContext);
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
            ContentSourceId = options.ContentSourceId,
            Title = metadata.Title,
            SermonCode = metadata.SermonCode,
            Year = metadata.Year,
            Date = metadata.Date,
            Location = metadata.Location,
            Language = metadata.Language,
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
            return null;
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
        CancellationToken cancellationToken)
    {
        if (SermonMetadataParser.IsBrotherBranhamSource(sourceContext))
        {
            return await EnsureBrotherBranhamAuthorExistsAsync(cancellationToken);
        }

        var authorMetadata = SermonMetadataParser.GetAuthorMetadata(sourceContext);
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
