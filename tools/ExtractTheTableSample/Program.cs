using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using IoDirectory = System.IO.Directory;

const string InfobasePath = @"C:\Users\hp\AppData\Local\VGR\infobases\eng-message-v95";
const string MetadataDbPath = @"C:\Users\hp\AppData\Local\VGR\infobases\eng-message-v95\metadataindex.db";
const string AppPath = @"C:\Program Files (x86)\VGR\The Table\1.24.2.7";
const string WebBuildPath = @"C:\Program Files (x86)\VGR\The Table\1.24.2.7\web\build";
const string OutputDirectory = @"D:\MessageFlow Archive\TheTableExtractionTest";
const string ReportPath = @"D:\MessageFlow Archive\TheTableExtractionTest\the_table_sample_extraction_report.txt";

var extractor = new TheTableSampleExtractor(
    InfobasePath,
    MetadataDbPath,
    AppPath,
    WebBuildPath,
    OutputDirectory,
    ReportPath);

return extractor.Run();

internal sealed class TheTableSampleExtractor(
    string infobasePath,
    string metadataDbPath,
    string appPath,
    string webBuildPath,
    string outputDirectory,
    string reportPath)
{
    private static readonly string[] TargetProductIds = ["47-0412", "63-0304", "65-1207"];

    private static readonly string[] WebSearchTerms =
    [
        "infobase",
        "metadataindex.db",
        "lucene",
        ".fdt",
        ".fdx",
        "segments",
        "sermon",
        "paragraph",
        "productId",
        "hasText"
    ];

    private static readonly string[] AssemblySearchTerms =
    [
        "LuceneInfobase",
        "TableSearchEngine",
        "GetTableDocumentBySermonId",
        "GetLuceneHtmlResponse",
        "GetLuceneHtml",
        "SermonHtml",
        "ContentsXhtml",
        "ParagraphNumber",
        "SecureNiofsDirectory",
        "VGR.Table.Text.Infobase",
        "GetEnglishSermonContent",
        "GetSermonContent",
        "GetEnglishText",
        "GetEngSubtitleIndexText",
        "metadataindex.db",
        "DirectoryReader",
        "NIOFSDirectory",
        "MMapDirectory"
    ];

    private static readonly string[] SelectedAssemblyNames =
    [
        "Table.dll",
        "VGR.Table.Common.dll",
        "VGR.Table.Core.dll",
        "VGR.Table.Lucene.dll"
    ];

    public int Run()
    {
        IoDirectory.CreateDirectory(outputDirectory);
        Batteries_V2.Init();

        var report = new StringBuilder();
        WriteHeader(report);

        var metadata = ReadMetadata(report);
        var inspection = InspectReaderLogic();
        AppendReaderInspection(report, inspection);

        var extraction = TryExtractSamples(metadata);
        AppendExtractionAttempt(report, extraction);

        WriteDisposableOutputs(report, metadata, extraction);
        File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        Console.WriteLine($"The Table sample extraction report written to: {reportPath}");
        Console.WriteLine(extraction.Paragraphs.Count == 0
            ? "No sample CSV files were written because no clean paragraph samples were extracted."
            : "Sample CSV files were written for extracted paragraph samples.");

        return 0;
    }

    private void WriteHeader(StringBuilder report)
    {
        report.AppendLine("MessageFlow - The Table Sample Extraction Prototype");
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        report.AppendLine("Safety");
        report.AppendLine("- Prototype only. No import was performed.");
        report.AppendLine("- The Table files were opened for read-only inspection only.");
        report.AppendLine("- The Table application was not launched.");
        report.AppendLine("- MessageFlow production database was not opened or modified.");
        report.AppendLine("- Existing Brother Branham and KJV Bible data were not opened or modified.");
        report.AppendLine("- No online services were scraped.");
        report.AppendLine("- No app-specific decoding, security bypass, DRM bypass, login bypass, or protected-content extraction was attempted.");
        report.AppendLine();
        report.AppendLine("Configured Paths");
        report.AppendLine($"- Infobase folder: {infobasePath} | exists: {IoDirectory.Exists(infobasePath)}");
        report.AppendLine($"- Metadata database: {metadataDbPath} | exists: {File.Exists(metadataDbPath)}");
        report.AppendLine($"- The Table app folder: {appPath} | exists: {IoDirectory.Exists(appPath)}");
        report.AppendLine($"- The Table web build folder: {webBuildPath} | exists: {IoDirectory.Exists(webBuildPath)}");
        report.AppendLine($"- Disposable output folder: {outputDirectory}");
        report.AppendLine();
    }

    private IReadOnlyList<SermonMetadata> ReadMetadata(StringBuilder report)
    {
        var sermons = new List<SermonMetadata>();

        report.AppendLine("Step 1 - Read Metadata");

        if (!File.Exists(metadataDbPath))
        {
            report.AppendLine("- Metadata database missing. No metadata could be read.");
            report.AppendLine();
            return sermons;
        }

        try
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = metadataDbPath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared
            };

            using var connection = new SqliteConnection(builder.ToString());
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT productIdentityId,
                       productId,
                       productTitle,
                       year,
                       location,
                       cityState,
                       dayOfWeek,
                       minutes,
                       hasText,
                       hasSubtitle,
                       publishedDate
                FROM SermonIndex_Eng
                WHERE productId IN ($p0, $p1, $p2)
                ORDER BY chronologicalSortId
                """;

            for (var i = 0; i < TargetProductIds.Length; i++)
            {
                command.Parameters.AddWithValue($"$p{i}", TargetProductIds[i]);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                sermons.Add(new SermonMetadata(
                    GetInt32(reader, "productIdentityId"),
                    GetString(reader, "productId"),
                    GetString(reader, "productTitle"),
                    GetString(reader, "year"),
                    GetString(reader, "location"),
                    GetString(reader, "cityState"),
                    GetString(reader, "dayOfWeek"),
                    GetInt32(reader, "minutes"),
                    GetInt32(reader, "hasText"),
                    GetInt32(reader, "hasSubtitle"),
                    GetInt64(reader, "publishedDate")));
            }

            report.AppendLine("- SQLite open mode: ReadOnly");
            report.AppendLine($"- Sermons requested: {string.Join(", ", TargetProductIds)}");
            report.AppendLine($"- Sermons found: {sermons.Count}");
            foreach (var target in TargetProductIds)
            {
                var sermon = sermons.SingleOrDefault(s => string.Equals(s.ProductId, target, StringComparison.OrdinalIgnoreCase));
                if (sermon is null)
                {
                    report.AppendLine($"  - {target}: Missing");
                    continue;
                }

                report.AppendLine(
                    $"  - {sermon.ProductId}: {sermon.Title}; year={sermon.Year}; location={sermon.Location}; productIdentityId={sermon.ProductIdentityId}; hasText={sermon.HasText}; hasSubtitle={sermon.HasSubtitle}");
            }
        }
        catch (Exception ex)
        {
            report.AppendLine($"- Metadata read failed: {ex.GetType().Name}: {ex.Message}");
        }

        report.AppendLine();
        return sermons;
    }

    private ReaderInspectionResult InspectReaderLogic()
    {
        var webMatches = new List<TextMatch>();
        var assemblyMatches = new List<AssemblyMatch>();
        var notes = new List<string>();

        if (!IoDirectory.Exists(webBuildPath))
        {
            notes.Add("Web build folder was not found.");
        }
        else
        {
            var webFiles = IoDirectory.EnumerateFiles(webBuildPath, "*.*", SearchOption.AllDirectories)
                .Where(IsReaderLogicCandidate)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var file in webFiles)
            {
                var match = SearchTextFile(file, WebSearchTerms);
                if (match is not null)
                {
                    webMatches.Add(match);
                }
            }

            if (webMatches.Count == 0)
            {
                notes.Add("No installed JS/HTML file directly referenced local Lucene segment or stored-field files.");
            }
        }

        foreach (var assemblyName in SelectedAssemblyNames)
        {
            var assemblyPath = Path.Combine(appPath, assemblyName);
            if (!File.Exists(assemblyPath))
            {
                assemblyMatches.Add(new AssemblyMatch(assemblyPath, false, []));
                continue;
            }

            assemblyMatches.Add(SearchAssemblyStrings(assemblyPath, AssemblySearchTerms));
        }

        return new ReaderInspectionResult(webMatches, assemblyMatches, notes);
    }

    private void AppendReaderInspection(StringBuilder report, ReaderInspectionResult inspection)
    {
        report.AppendLine("Step 2 - Inspect The Table App Reader Logic");
        report.AppendLine("Scope: local installed files only. The Table was not launched and no files were modified.");
        report.AppendLine();

        report.AppendLine("Web build search");
        if (inspection.WebMatches.Count == 0)
        {
            report.AppendLine("- No relevant JS/HTML matches found outside locale/asset text.");
        }
        else
        {
            foreach (var match in inspection.WebMatches.Take(25))
            {
                report.AppendLine($"- {match.Path}");
                report.AppendLine($"  Terms: {string.Join(", ", match.Terms)}");
                report.AppendLine($"  Example: {match.Snippet}");
            }

            if (inspection.WebMatches.Count > 25)
            {
                report.AppendLine($"- Web match list truncated. Total matches: {inspection.WebMatches.Count}");
            }
        }

        report.AppendLine();
        report.AppendLine("Selected assembly string search");
        foreach (var match in inspection.AssemblyMatches)
        {
            report.AppendLine($"- {match.Path}");
            if (!match.Exists)
            {
                report.AppendLine("  Exists: No");
            }
            else if (match.Terms.Count == 0)
            {
                report.AppendLine("  Exists: Yes; no searched reader terms found.");
            }
            else
            {
                report.AppendLine($"  Exists: Yes; terms: {string.Join(", ", match.Terms)}");
            }
        }

        foreach (var note in inspection.Notes)
        {
            report.AppendLine($"- Note: {note}");
        }

        report.AppendLine("- Inference: the installed web UI appears to rely on The Table's local .NET/CefSharp bridge for sermon text; selected VGR assemblies contain Lucene and sermon-content reader names.");
        report.AppendLine("- Safety decision: app-specific reader internals or transformed index bytes were not used to decode protected/proprietary storage.");
        report.AppendLine();
    }

    private ExtractionAttempt TryExtractSamples(IReadOnlyList<SermonMetadata> metadata)
    {
        var notes = new List<string>();
        var paragraphs = new List<ExtractedParagraph>();

        if (!IoDirectory.Exists(infobasePath))
        {
            notes.Add("Infobase folder missing. No text extraction attempt could run.");
            return new ExtractionAttempt(paragraphs, notes);
        }

        var luceneResult = TryOpenStandardLuceneIndex(metadata);
        notes.AddRange(luceneResult.Notes);
        paragraphs.AddRange(luceneResult.Paragraphs);

        if (paragraphs.Count == 0)
        {
            notes.AddRange(ScanForPlainReadableEvidence(metadata));
        }

        return new ExtractionAttempt(paragraphs, notes);
    }

    private ExtractionAttempt TryOpenStandardLuceneIndex(IReadOnlyList<SermonMetadata> metadata)
    {
        var notes = new List<string>();
        var paragraphs = new List<ExtractedParagraph>();

        try
        {
            using var directory = FSDirectory.Open(new DirectoryInfo(infobasePath));
            using var reader = DirectoryReader.Open(directory);

            notes.Add($"Standard Lucene.NET opened the index read-only. Documents: {reader.NumDocs}; maxDoc: {reader.MaxDoc}.");
            paragraphs.AddRange(ExtractStoredSamples(reader, metadata, notes));

            if (paragraphs.Count == 0)
            {
                notes.Add("Standard Lucene.NET opened the index, but no clean stored paragraph samples were identified.");
            }
        }
        catch (Exception ex)
        {
            notes.Add($"Standard Lucene.NET read-only open failed: {ex.GetType().Name}: {ex.Message}");
            notes.Add("This matches the earlier inspection result: the local infobase is not readable as a normal Lucene.NET index.");
        }

        return new ExtractionAttempt(paragraphs, notes);
    }

    private static IReadOnlyList<ExtractedParagraph> ExtractStoredSamples(
        DirectoryReader reader,
        IReadOnlyList<SermonMetadata> metadata,
        ICollection<string> notes)
    {
        var samples = new List<ExtractedParagraph>();
        var samplesByProduct = TargetProductIds.ToDictionary(productId => productId, _ => 0, StringComparer.OrdinalIgnoreCase);
        var maxDocsToInspect = Math.Min(reader.MaxDoc, 50_000);

        for (var docId = 0; docId < maxDocsToInspect && samplesByProduct.Values.Any(count => count < 5); docId++)
        {
            var document = reader.Document(docId);
            var fields = document.Fields
                .Select(field => new DocumentField(field.Name, field.GetStringValue() ?? string.Empty))
                .Where(field => !string.IsNullOrWhiteSpace(field.Value))
                .ToArray();

            if (fields.Length == 0)
            {
                continue;
            }

            var combined = string.Join("\n", fields.Select(field => field.Value));
            foreach (var sermon in metadata)
            {
                if (samplesByProduct[sermon.ProductId] >= 5)
                {
                    continue;
                }

                if (!DocumentLooksLikeSermon(fields, combined, sermon))
                {
                    continue;
                }

                foreach (var candidate in ExtractParagraphCandidates(fields, combined, sermon))
                {
                    if (samplesByProduct[sermon.ProductId] >= 5)
                    {
                        break;
                    }

                    if (!IsCleanParagraph(candidate.ParagraphText))
                    {
                        continue;
                    }

                    samples.Add(candidate);
                    samplesByProduct[sermon.ProductId]++;
                }
            }
        }

        if (reader.MaxDoc > maxDocsToInspect)
        {
            notes.Add($"Stored-document inspection was capped at {maxDocsToInspect.ToString(CultureInfo.InvariantCulture)} documents to keep the prototype small.");
        }

        return samples
            .GroupBy(p => p.ProductId, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => group
                .OrderBy(p => ParseParagraphSortValue(p.ParagraphNumber))
                .Take(5))
            .ToArray();
    }

    private static bool DocumentLooksLikeSermon(
        IReadOnlyCollection<DocumentField> fields,
        string combined,
        SermonMetadata sermon)
    {
        if (combined.Contains(sermon.ProductId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(sermon.Title) &&
            combined.Contains(sermon.Title, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return fields.Any(field =>
            field.Value.Equals(sermon.ProductIdentityId.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase) &&
            FieldNameLooksLikeIdentity(field.Name));
    }

    private static IEnumerable<ExtractedParagraph> ExtractParagraphCandidates(
        IReadOnlyCollection<DocumentField> fields,
        string combined,
        SermonMetadata sermon)
    {
        var paragraphNumber = fields
            .FirstOrDefault(field => FieldNameLooksLikeParagraphNumber(field.Name) && field.Value.Length <= 24)
            ?.Value ?? string.Empty;

        foreach (var field in fields.Where(field => FieldNameLooksLikeBodyText(field.Name)))
        {
            foreach (var paragraph in ParseParagraphs(field.Value, paragraphNumber))
            {
                yield return new ExtractedParagraph(
                    sermon.ProductId,
                    sermon.Title,
                    sermon.Year,
                    sermon.Location,
                    paragraph.Number,
                    paragraph.Text);
            }
        }

        if (!fields.Any(field => FieldNameLooksLikeBodyText(field.Name)))
        {
            foreach (var paragraph in ParseParagraphs(combined, paragraphNumber))
            {
                yield return new ExtractedParagraph(
                    sermon.ProductId,
                    sermon.Title,
                    sermon.Year,
                    sermon.Location,
                    paragraph.Number,
                    paragraph.Text);
            }
        }
    }

    private static IEnumerable<(string Number, string Text)> ParseParagraphs(string value, string fallbackNumber)
    {
        var text = StripHtml(value);
        var matches = Regex.Matches(
            text,
            @"(?ms)(?<num>\d{1,4}(?:-\d{1,4})?|\d{2,3}\.\d{1,3})\s+(?<text>[A-Z""'(\[][^\r\n]{35,2000}?)(?=\r?\n\s*(?:\d{1,4}(?:-\d{1,4})?|\d{2,3}\.\d{1,3})\s+[A-Z""'(\[]|\z)");

        foreach (Match match in matches)
        {
            yield return (match.Groups["num"].Value.Trim(), NormalizeWhitespace(match.Groups["text"].Value));
        }

        if (matches.Count == 0 && !string.IsNullOrWhiteSpace(fallbackNumber) && IsCleanParagraph(text))
        {
            yield return (fallbackNumber.Trim(), NormalizeWhitespace(text));
        }
    }

    private IReadOnlyList<string> ScanForPlainReadableEvidence(IReadOnlyList<SermonMetadata> metadata)
    {
        var notes = new List<string>();
        var filesToScan = IoDirectory.EnumerateFiles(infobasePath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                var extension = Path.GetExtension(path);
                return extension.Equals(".fdt", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".fdx", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".cfs", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".cfe", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".si", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".doc", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".pos", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".tim", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".tip", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".proto", StringComparison.OrdinalIgnoreCase) ||
                       extension.Equals(".db", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var sermon in metadata)
        {
            var matchingFiles = filesToScan
                .Where(path => FileContainsAscii(path, sermon.ProductId) || FileContainsAscii(path, sermon.Title))
                .Select(path => Path.GetFileName(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            notes.Add(matchingFiles.Length == 0
                ? $"Plain byte scan: {sermon.ProductId} metadata/title markers were not found in top-level infobase data files."
                : $"Plain byte scan: {sermon.ProductId} metadata/title markers found in {string.Join(", ", matchingFiles)}.");
        }

        var fdtPath = IoDirectory.EnumerateFiles(infobasePath, "*.fdt", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (fdtPath is not null)
        {
            var fdtFile = new FileInfo(fdtPath);
            notes.Add($"Stored-field file present: {fdtPath} ({FormatBytes(fdtFile.Length)}). No plain readable sermon paragraph samples were accepted from normal scans.");
        }

        return notes;
    }

    private void AppendExtractionAttempt(StringBuilder report, ExtractionAttempt extraction)
    {
        report.AppendLine("Step 3 - Tiny Sample Extraction Attempt");

        foreach (var note in extraction.Notes)
        {
            report.AppendLine($"- {note}");
        }

        if (extraction.Paragraphs.Count == 0)
        {
            report.AppendLine("- Result: no first-five paragraph samples were extracted.");
        }
        else
        {
            foreach (var group in extraction.Paragraphs.GroupBy(p => p.ProductId, StringComparer.OrdinalIgnoreCase))
            {
                report.AppendLine($"- {group.Key}: extracted {group.Count()} paragraph sample(s).");
                foreach (var paragraph in group.Take(5))
                {
                    report.AppendLine($"  - Paragraph {paragraph.ParagraphNumber}: {TrimForReport(paragraph.ParagraphText, 180)}");
                }
            }
        }

        report.AppendLine();
        report.AppendLine("Step 5 - Quality Checks");
        if (extraction.Paragraphs.Count == 0)
        {
            report.AppendLine("- Readable English paragraph text: Not passed; no body paragraphs were extracted.");
            report.AppendLine("- Paragraph number order: Not applicable; no paragraph numbers were extracted.");
            report.AppendLine("- Junk/binary symbols: Passed for output; no questionable paragraph output was written.");
            report.AppendLine("- Empty/UI/header/footer exclusion: Passed for output; no paragraph output was written.");
        }
        else
        {
            var groups = extraction.Paragraphs
                .GroupBy(p => p.ProductId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var ordered = groups.All(group => IsStrictlyIncreasing(group.Select(p => ParseParagraphSortValue(p.ParagraphNumber))));
            var cleanText = extraction.Paragraphs.All(p => IsCleanParagraph(p.ParagraphText));

            report.AppendLine($"- Readable English paragraph text: {(cleanText ? "Passed" : "Failed")}");
            report.AppendLine($"- Paragraph number order: {(ordered ? "Passed" : "Failed")}");
            report.AppendLine("- Junk/binary symbols: excluded by prototype filters.");
            report.AppendLine("- Empty/UI/header/footer text: excluded by prototype filters.");
        }

        report.AppendLine();
    }

    private void WriteDisposableOutputs(
        StringBuilder report,
        IReadOnlyList<SermonMetadata> metadata,
        ExtractionAttempt extraction)
    {
        report.AppendLine("Step 4 - Disposable Outputs");
        report.AppendLine($"- Report: {reportPath}");

        if (extraction.Paragraphs.Count == 0)
        {
            report.AppendLine("- CSV files: none written, because no clean paragraph samples were extracted.");
            AppendDecision(report, metadata, extraction, "C. Extraction not possible / not recommended");
            return;
        }

        var byProduct = extraction.Paragraphs
            .GroupBy(p => p.ProductId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        if (byProduct.TryGetValue("47-0412", out var faithSamples) && faithSamples.Length > 0)
        {
            var path = Path.Combine(outputDirectory, "the_table_sample_47-0412.csv");
            WriteCsv(path, faithSamples);
            report.AppendLine($"- CSV written: {path}");
        }

        if (byProduct.Count > 1)
        {
            var path = Path.Combine(outputDirectory, "the_table_sample_all.csv");
            WriteCsv(path, extraction.Paragraphs);
            report.AppendLine($"- CSV written: {path}");
        }

        AppendDecision(report, metadata, extraction, "A. Extraction successful and clean");
    }

    private void AppendDecision(
        StringBuilder report,
        IReadOnlyList<SermonMetadata> metadata,
        ExtractionAttempt extraction,
        string conclusion)
    {
        report.AppendLine();
        report.AppendLine("Step 6 - Import Realism Decision");
        report.AppendLine($"- Conclusion: {conclusion}");

        if (extraction.Paragraphs.Count == 0)
        {
            report.AppendLine("- What worked: read-only metadata access succeeded for the available requested sermons.");
            report.AppendLine("- What failed: clean paragraph text was not available through normal SQLite, plain local text, or standard Lucene.NET read-only access.");
            report.AppendLine("- Exact files used for evidence:");
            report.AppendLine($"  - {metadataDbPath}");
            report.AppendLine($"  - {infobasePath}");
            report.AppendLine($"  - {webBuildPath}");
            foreach (var assemblyName in SelectedAssemblyNames)
            {
                report.AppendLine($"  - {Path.Combine(appPath, assemblyName)}");
            }
            report.AppendLine("- Reason: sermon body text appears to be stored in app-specific/proprietary Lucene-related files, with reader names present in VGR assemblies rather than ordinary JS or SQLite tables.");
            report.AppendLine("- Recommendation: do not build an importer from The Table internals under the current safety rules. The next safe path is an authorized VGR export/API/permissioned data source, or continuing with MessageFlow's existing PDF-derived Brother Branham data.");
        }
        else
        {
            report.AppendLine("- Exact files used:");
            report.AppendLine($"  - {metadataDbPath}");
            report.AppendLine($"  - {infobasePath}");
            report.AppendLine("- Mapping to MessageFlow:");
            report.AppendLine("  - productId -> sermon code");
            report.AppendLine("  - productTitle -> title");
            report.AppendLine("  - year/location -> sermon metadata");
            report.AppendLine("  - paragraphNumber/paragraphText -> ordered sermon paragraph rows");
            report.AppendLine("- Recommendation: build only a disposable test database importer next, then compare against existing Brother Branham data before any production import is considered.");
        }

        report.AppendLine();
    }

    private static void WriteCsv(string path, IEnumerable<ExtractedParagraph> paragraphs)
    {
        using var writer = new StreamWriter(path, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        writer.WriteLine("productId,title,year,location,paragraphNumber,paragraphText");
        foreach (var paragraph in paragraphs)
        {
            writer.WriteLine(string.Join(
                ",",
                EscapeCsv(paragraph.ProductId),
                EscapeCsv(paragraph.Title),
                EscapeCsv(paragraph.Year),
                EscapeCsv(paragraph.Location),
                EscapeCsv(paragraph.ParagraphNumber),
                EscapeCsv(paragraph.ParagraphText)));
        }
    }

    private static string EscapeCsv(string value)
    {
        var escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{escaped}\"";
    }

    private static bool IsReaderLogicCandidate(string path)
    {
        var extension = Path.GetExtension(path);
        if (!extension.Equals(".js", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalized = path.Replace('\\', '/');
        return !normalized.Contains("/locales/", StringComparison.OrdinalIgnoreCase) &&
               !normalized.Contains("/fonts/", StringComparison.OrdinalIgnoreCase) &&
               !normalized.Contains("/assets/", StringComparison.OrdinalIgnoreCase);
    }

    private static TextMatch? SearchTextFile(string path, IReadOnlyCollection<string> terms)
    {
        try
        {
            var text = File.ReadAllText(path);
            var matchedTerms = terms
                .Where(term => text.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matchedTerms.Length == 0)
            {
                return null;
            }

            var firstTerm = matchedTerms[0];
            var index = text.IndexOf(firstTerm, StringComparison.OrdinalIgnoreCase);
            var start = Math.Max(0, index - 100);
            var length = Math.Min(text.Length - start, firstTerm.Length + 220);
            var snippet = NormalizeWhitespace(text.Substring(start, length));

            return new TextMatch(path, matchedTerms, snippet);
        }
        catch (Exception ex)
        {
            return new TextMatch(path, ["read failed"], $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static AssemblyMatch SearchAssemblyStrings(string path, IReadOnlyCollection<string> terms)
    {
        if (!File.Exists(path))
        {
            return new AssemblyMatch(path, false, []);
        }

        var bytes = File.ReadAllBytes(path);
        var ascii = Encoding.Latin1.GetString(bytes);
        var utf16 = Encoding.Unicode.GetString(bytes);
        var matchedTerms = terms
            .Where(term =>
                ascii.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                utf16.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AssemblyMatch(path, true, matchedTerms);
    }

    private static bool FileContainsAscii(string path, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !File.Exists(path))
        {
            return false;
        }

        var needle = Encoding.UTF8.GetBytes(value);
        if (needle.Length == 0)
        {
            return false;
        }

        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var buffer = new byte[64 * 1024];
        var carry = Array.Empty<byte>();

        while (true)
        {
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                return false;
            }

            var searchable = new byte[carry.Length + read];
            Buffer.BlockCopy(carry, 0, searchable, 0, carry.Length);
            Buffer.BlockCopy(buffer, 0, searchable, carry.Length, read);

            if (IndexOf(searchable, needle) >= 0)
            {
                return true;
            }

            var carryLength = Math.Min(needle.Length - 1, searchable.Length);
            carry = searchable[^carryLength..];
        }
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            var found = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] == needle[j])
                {
                    continue;
                }

                found = false;
                break;
            }

            if (found)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool FieldNameLooksLikeIdentity(string name)
    {
        return name.Contains("product", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("sermon", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("identity", StringComparison.OrdinalIgnoreCase);
    }

    private static bool FieldNameLooksLikeParagraphNumber(string name)
    {
        return name.Contains("paragraph", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("subtitle", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("number", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("pid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool FieldNameLooksLikeBodyText(string name)
    {
        return name.Contains("text", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("html", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("xhtml", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("content", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("body", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCleanParagraph(string text)
    {
        var normalized = NormalizeWhitespace(StripHtml(text));
        if (normalized.Length < 35)
        {
            return false;
        }

        if (normalized.Contains('\0') ||
            normalized.Contains("function ", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("var ", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Copyright", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("DOCTYPE", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var letters = normalized.Count(char.IsLetter);
        var controls = normalized.Count(char.IsControl);
        var asciiPrintable = normalized.Count(c => c is >= ' ' and <= '~');
        var letterRatio = letters / (double)normalized.Length;
        var printableRatio = asciiPrintable / (double)normalized.Length;

        return controls == 0 && letterRatio >= 0.45 && printableRatio >= 0.85;
    }

    private static bool IsStrictlyIncreasing(IEnumerable<double> values)
    {
        var previous = double.NegativeInfinity;
        foreach (var value in values)
        {
            if (value <= previous)
            {
                return false;
            }

            previous = value;
        }

        return true;
    }

    private static double ParseParagraphSortValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return double.NaN;
        }

        var normalized = value.Trim().Replace('-', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : double.NaN;
    }

    private static string StripHtml(string value)
    {
        var withoutTags = Regex.Replace(value, "<[^>]+>", " ");
        return System.Net.WebUtility.HtmlDecode(withoutTags);
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static string TrimForReport(string value, int maxLength)
    {
        var normalized = NormalizeWhitespace(value);
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength] + "...";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes.ToString(CultureInfo.InvariantCulture)} B";
        }

        var kib = bytes / 1024d;
        if (kib < 1024)
        {
            return $"{kib.ToString("0.##", CultureInfo.InvariantCulture)} KB";
        }

        return $"{(kib / 1024d).ToString("0.##", CultureInfo.InvariantCulture)} MB";
    }

    private static string GetString(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static int GetInt32(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static long GetInt64(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt64(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
}

internal sealed record SermonMetadata(
    int ProductIdentityId,
    string ProductId,
    string Title,
    string Year,
    string Location,
    string CityState,
    string DayOfWeek,
    int Minutes,
    int HasText,
    int HasSubtitle,
    long PublishedDate);

internal sealed record ExtractedParagraph(
    string ProductId,
    string Title,
    string Year,
    string Location,
    string ParagraphNumber,
    string ParagraphText);

internal sealed record ReaderInspectionResult(
    IReadOnlyList<TextMatch> WebMatches,
    IReadOnlyList<AssemblyMatch> AssemblyMatches,
    IReadOnlyList<string> Notes);

internal sealed record TextMatch(string Path, IReadOnlyList<string> Terms, string Snippet);

internal sealed record AssemblyMatch(string Path, bool Exists, IReadOnlyList<string> Terms);

internal sealed record ExtractionAttempt(IReadOnlyList<ExtractedParagraph> Paragraphs, IReadOnlyList<string> Notes);

internal sealed record DocumentField(string Name, string Value);
