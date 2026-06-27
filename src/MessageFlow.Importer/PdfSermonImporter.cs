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
        var pdfFiles = Directory.EnumerateFiles(options.SourceRoot, "*.pdf", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var summary = new ImportSummary
        {
            TotalFiles = pdfFiles.Count
        };

        Console.WriteLine($"PDF files found: {summary.TotalFiles}");
        Console.WriteLine();

        await EnsureAuthorExistsAsync(cancellationToken);
        if (options.Reset)
        {
            await ResetImportedSermonsAsync(options.SourceRoot, cancellationToken);
        }

        for (var index = 0; index < pdfFiles.Count; index++)
        {
            var filePath = Path.GetFullPath(pdfFiles[index]);
            Console.WriteLine($"[{index + 1}/{summary.TotalFiles}] {filePath}");

            try
            {
                var result = await ImportFileAsync(filePath, options, cancellationToken);

                if (result.Skipped)
                {
                    summary.SkippedFiles++;
                    Console.WriteLine("  skipped: already imported");
                    continue;
                }

                summary.ImportedFiles++;
                summary.ImportedParagraphs += result.ParagraphCount;
                Console.WriteLine($"  imported paragraphs: {result.ParagraphCount}");
            }
            catch (Exception ex)
            {
                summary.ErrorCount++;
                dbContext.ChangeTracker.Clear();
                await WriteImportLogAsync(filePath, "Error", ex.Message, cancellationToken);
                Console.WriteLine($"  error: {ex.Message}");
            }
        }

        return summary;
    }

    private async Task<ImportFileResult> ImportFileAsync(
        string filePath,
        ImportOptions options,
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
        var paragraphs = ParagraphSplitter.Split(pages);
        var metadata = SermonMetadataParser.Parse(filePath, options.SourceRoot);
        var extractedCharacterCount = pages.Sum(page => page.Text.Length);
        var detectedParagraphNumbers = paragraphs.Count(paragraph => paragraph.HasDetectedParagraphNumber);
        var fallbackParagraphNumbers = paragraphs.Count - detectedParagraphNumbers;
        var preview = TextCleaner.BuildPreview(string.Join(" ", paragraphs.Select(paragraph => paragraph.Text)), 200);

        Console.WriteLine($"  file: {Path.GetFileName(filePath)}");
        Console.WriteLine($"  extracted characters: {extractedCharacterCount:N0}");
        Console.WriteLine($"  paragraph count: {paragraphs.Count:N0}");
        Console.WriteLine($"  detected paragraph numbers: {detectedParagraphNumbers:N0}");
        Console.WriteLine($"  fallback paragraph numbers: {fallbackParagraphNumbers:N0}");
        Console.WriteLine($"  preview: {preview}");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (existingSermon is not null)
        {
            dbContext.Sermons.Remove(new Sermon { Id = existingSermon.Id });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var sermon = new Sermon
        {
            AuthorId = AuthorId,
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
            Message = $"Imported {paragraphs.Count} paragraphs. Detected numbers: {detectedParagraphNumbers}. Fallback numbers: {fallbackParagraphNumbers}.",
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

    private async Task EnsureAuthorExistsAsync(CancellationToken cancellationToken)
    {
        var exists = await dbContext.Authors.AnyAsync(author => author.Id == AuthorId, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.Authors.Add(new Author
        {
            Id = AuthorId,
            FullName = "William Marrion Branham",
            DisplayName = "Brother Branham",
            Description = "Primary sermon author for the local MessageFlow sermon library."
        });

        await dbContext.SaveChangesAsync(cancellationToken);
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

    private sealed record ImportFileResult(bool Skipped, int ParagraphCount)
    {
        public static ImportFileResult Skip { get; } = new(true, 0);
    }
}
