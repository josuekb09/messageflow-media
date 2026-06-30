using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MessageFlow.Importer;

const string circularLettersRoot = @"D:\Ewald Frank\Circular Letters";
const string booksAndBrochuresRoot = @"D:\Ewald Frank\Books and Brochures";
const string trackerFolder = @"D:\Ewald Frank\_Download Tracker";
const string circularDestination = @"D:\Ewald Frank\_Organized\Circular Letters\English";
const string booksDestination = @"D:\Ewald Frank\_Organized\Books and Brochures\English";
const string unknownDestination = @"D:\Ewald Frank\_Organized\Unknown";
const string previewCsvPath = @"D:\Ewald Frank\_Download Tracker\ewald_frank_pdf_metadata_preview.csv";
const string previewTextPath = @"D:\Ewald Frank\_Download Tracker\ewald_frank_pdf_metadata_preview.txt";
const string appliedCsvPath = @"D:\Ewald Frank\_Download Tracker\ewald_frank_pdf_metadata_applied.csv";
const string MonthPattern =
    "jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t|tember)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?";

var apply = args.Any(argument => string.Equals(argument, "--apply", StringComparison.OrdinalIgnoreCase));
var unknownArguments = args
    .Where(argument => !string.Equals(argument, "--apply", StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(argument, "--help", StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(argument, "-h", StringComparison.OrdinalIgnoreCase))
    .ToList();

if (args.Any(argument => string.Equals(argument, "--help", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(argument, "-h", StringComparison.OrdinalIgnoreCase)))
{
    PrintUsage();
    return 0;
}

if (unknownArguments.Count > 0)
{
    Console.WriteLine($"Unknown argument: {unknownArguments[0]}");
    PrintUsage();
    return 2;
}

Directory.CreateDirectory(trackerFolder);

var pdfFiles = EnumerateSourcePdfs()
    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
    .ToList();

Console.WriteLine("MessageFlow Brother Frank PDF Organizer");
Console.WriteLine(apply ? "Mode: apply copy only" : "Mode: dry run preview only");
Console.WriteLine($"PDF files found: {pdfFiles.Count:N0}");

var extractor = new PdfTextExtractor();
var records = new List<PdfMetadataRecord>(pdfFiles.Count);

for (var index = 0; index < pdfFiles.Count; index++)
{
    var filePath = pdfFiles[index];
    Console.WriteLine($"[{index + 1:N0}/{pdfFiles.Count:N0}] Reading {Path.GetFileName(filePath)}");
    records.Add(AnalyzePdf(filePath, extractor));
}

var validation = Validate(records);
WritePreviewCsv(records, previewCsvPath);
WriteTextReport(records, validation, previewTextPath, apply);

AppliedRecord[] appliedRecords = [];
if (apply)
{
    appliedRecords = ApplyCopies(records);
    WriteAppliedCsv(appliedRecords, appliedCsvPath);
}

PrintSummary(records, validation, apply, appliedRecords.Length);
return validation.HasBlockingFailures && apply ? 1 : 0;

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine(@"  dotnet run --project tools\OrganizeEwaldFrankPdfs");
    Console.WriteLine(@"  dotnet run --project tools\OrganizeEwaldFrankPdfs -- --apply");
}

static IEnumerable<string> EnumerateSourcePdfs()
{
    foreach (var root in new[] { circularLettersRoot, booksAndBrochuresRoot })
    {
        if (!Directory.Exists(root))
        {
            Console.WriteLine($"Source folder not found: {root}");
            continue;
        }

        foreach (var filePath in Directory.EnumerateFiles(root, "*.pdf", SearchOption.AllDirectories))
        {
            yield return filePath;
        }
    }
}

static PdfMetadataRecord AnalyzePdf(string filePath, PdfTextExtractor extractor)
{
    var warnings = new List<string>();
    var fileName = Path.GetFileName(filePath);
    var currentFolder = Path.GetDirectoryName(filePath) ?? string.Empty;
    var pageCount = 0;
    var firstPagesText = string.Empty;
    var extractionSucceeded = false;

    try
    {
        var pages = extractor.ExtractPages(filePath);
        pageCount = pages.Count;
        firstPagesText = string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            pages.Take(Math.Min(5, pages.Count)).Select(page => page.Text));
        extractionSucceeded = !string.IsNullOrWhiteSpace(firstPagesText);
        if (!extractionSucceeded)
        {
            warnings.Add("PDF text extraction returned no readable text.");
        }
    }
    catch (Exception ex)
    {
        warnings.Add($"PDF text extraction failed: {ex.Message}");
    }

    var category = DetectCategory(filePath, firstPagesText, pageCount);
    var language = DetectLanguage(fileName, firstPagesText, warnings);
    var date = category == "Circular Letter"
        ? DetectCircularDate(fileName, firstPagesText, warnings)
        : DateDetection.None;
    var titleDetection = DetectTitle(fileName, firstPagesText, category, date, warnings);
    var topicNote = CreateTopicNote(category, titleDetection.Title, firstPagesText);
    var destination = category switch
    {
        "Circular Letter" => circularDestination,
        "Book" or "Brochure" => booksDestination,
        _ => unknownDestination
    };
    var suggestedFileName = CreateSuggestedFileName(category, titleDetection.Title, date, fileName);
    var confidence = DetermineConfidence(
        extractionSucceeded,
        category,
        language,
        date,
        titleDetection,
        warnings);

    return new PdfMetadataRecord(
        filePath,
        fileName,
        currentFolder,
        category,
        language,
        date.Display,
        titleDetection.Title,
        topicNote,
        suggestedFileName,
        destination,
        confidence,
        string.Join("; ", warnings));
}

static string DetectCategory(string filePath, string firstPagesText, int pageCount)
{
    var folder = Path.GetDirectoryName(filePath) ?? string.Empty;
    var fileName = Path.GetFileNameWithoutExtension(filePath);
    var combined = NormalizeForMatching($"{folder} {fileName} {firstPagesText}");

    if (NormalizeForMatching(folder).Contains("circular letters", StringComparison.Ordinal) ||
        combined.Contains("circular letter", StringComparison.Ordinal) ||
        Regex.IsMatch(combined, @"\bcircular\b", RegexOptions.IgnoreCase))
    {
        return "Circular Letter";
    }

    if (!NormalizeForMatching(folder).Contains("books and brochures", StringComparison.Ordinal))
    {
        return "Unknown";
    }

    var knownTitle = TryMapKnownTitle(fileName);
    if (!string.IsNullOrWhiteSpace(knownTitle))
    {
        return DetectKnownTitleCategory(knownTitle);
    }

    if (Regex.IsMatch(
            combined,
            @"\b(baptism|footwashing|bride church|global information|vision 7000|pakistan february|newsletter)\b",
            RegexOptions.IgnoreCase))
    {
        return "Brochure";
    }

    if (Regex.IsMatch(
            combined,
            @"\b(return of christ|god and his plan|most read book|traditional christianity|people ask questions|revelation|seven seals|sixty years)\b",
            RegexOptions.IgnoreCase))
    {
        return "Book";
    }

    return pageCount >= 36 ? "Book" : pageCount > 0 ? "Brochure" : "Unknown";
}

static string DetectLanguage(string fileName, string firstPagesText, List<string> warnings)
{
    if (fileName.StartsWith("en-", StringComparison.OrdinalIgnoreCase))
    {
        return "English";
    }

    var normalized = NormalizeForMatching(firstPagesText);
    var englishSignals = new[]
    {
        " the ",
        " and ",
        " god ",
        " lord ",
        " christ ",
        " church ",
        " word ",
        " scripture ",
        " brother "
    };
    var signalCount = englishSignals.Count(signal => normalized.Contains(signal, StringComparison.Ordinal));
    if (signalCount >= 3)
    {
        return "English";
    }

    warnings.Add("Language could not be proven from filename or extracted text; assumed English for this preview.");
    return "English";
}

static DateDetection DetectCircularDate(string fileName, string firstPagesText, List<string> warnings)
{
    var fileStem = NormalizeDashes(Path.GetFileNameWithoutExtension(fileName));
    var fileStemDate = DetectDateFromText(fileStem);
    if (fileStemDate.IsConfident)
    {
        return fileStemDate;
    }

    var searchText = NormalizeDashes($"{fileStem} {Environment.NewLine}{firstPagesText}");
    var anyTextDate = DetectDateFromText(searchText);
    if (anyTextDate.IsConfident)
    {
        return anyTextDate;
    }

    warnings.Add("Circular letter date could not be detected with confidence.");
    return DateDetection.None;
}

static DateDetection DetectDateFromText(string searchText)
{
    var numericYearMonthMatch = Regex.Match(
        searchText,
        @"\b(?<year>19\d{2}|20\d{2})[-_\s](?<month>0?[1-9]|1[0-2])\b",
        RegexOptions.IgnoreCase);
    if (numericYearMonthMatch.Success &&
        int.TryParse(numericYearMonthMatch.Groups["month"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericMonth) &&
        TryGetMonthByNumber(numericMonth, out var numericMonthInfo))
    {
        var year = numericYearMonthMatch.Groups["year"].Value;
        return new DateDetection($"{numericMonthInfo.Name} {year}", $"{year}-{numericMonthInfo.Number:00}", true);
    }

    var seasonMatch = Regex.Match(
        searchText,
        @"\b(?:(?<season>spring|summer|autumn|fall|winter)\s*(?<year>19\d{2}|20\d{2})|(?<year2>19\d{2}|20\d{2})[-_\s]*(?<season2>spring|summer|autumn|fall|winter))\b",
        RegexOptions.IgnoreCase);
    if (seasonMatch.Success)
    {
        var season = seasonMatch.Groups["season"].Success
            ? seasonMatch.Groups["season"].Value
            : seasonMatch.Groups["season2"].Value;
        var year = seasonMatch.Groups["year"].Success
            ? seasonMatch.Groups["year"].Value
            : seasonMatch.Groups["year2"].Value;
        var displaySeason = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(season.ToLowerInvariant());
        return new DateDetection($"{displaySeason} {year}", $"{year}-{displaySeason}", true);
    }

    var yearEndMatch = Regex.Match(
        searchText,
        @"\byear\s*end\s+(?<year>19\d{2}|20\d{2})\b",
        RegexOptions.IgnoreCase);
    if (yearEndMatch.Success)
    {
        var year = yearEndMatch.Groups["year"].Value;
        return new DateDetection($"Year End {year}", $"{year}-Year-End", true);
    }

    var rangeMatch = Regex.Match(
        searchText,
        $@"\b(?<first>{MonthPattern})\s*[-/]\s*(?<second>{MonthPattern})\s+(?<year>19\d{{2}}|20\d{{2}})\b",
        RegexOptions.IgnoreCase);
    if (rangeMatch.Success &&
        TryGetMonth(rangeMatch.Groups["first"].Value, out var firstMonth) &&
        TryGetMonth(rangeMatch.Groups["second"].Value, out var secondMonth))
    {
        var year = rangeMatch.Groups["year"].Value;
        return new DateDetection(
            $"{firstMonth.Name}-{secondMonth.Name} {year}",
            $"{year}-{firstMonth.Number:00}-{secondMonth.Number:00}",
            true);
    }

    var singleMatch = Regex.Match(
        searchText,
        $@"\b(?<month>{MonthPattern})\s+(?<year>19\d{{2}}|20\d{{2}})\b",
        RegexOptions.IgnoreCase);
    if (singleMatch.Success && TryGetMonth(singleMatch.Groups["month"].Value, out var month))
    {
        var year = singleMatch.Groups["year"].Value;
        return new DateDetection($"{month.Name} {year}", $"{year}-{month.Number:00}", true);
    }

    return DateDetection.None;
}

static string DetectKnownTitleCategory(string knownTitle)
{
    return NormalizeForMatching(knownTitle) switch
    {
        var title when title.Contains(" baptism lord s supper footwashing ", StringComparison.Ordinal) => "Brochure",
        var title when title.Contains(" global information ", StringComparison.Ordinal) => "Brochure",
        var title when title.Contains(" pakistan february 2010 ", StringComparison.Ordinal) => "Brochure",
        var title when title.Contains(" to the bride church of jesus christ ", StringComparison.Ordinal) => "Brochure",
        var title when title.Contains(" vision 7000 ", StringComparison.Ordinal) => "Brochure",
        var title when title.Contains(" sixty years in the service of the lord ", StringComparison.Ordinal) => "Brochure",
        _ => "Book"
    };
}

static TitleDetection DetectTitle(
    string fileName,
    string firstPagesText,
    string category,
    DateDetection date,
    List<string> warnings)
{
    if (category == "Circular Letter")
    {
        if (date.IsConfident)
        {
            return new TitleDetection($"Circular Letter - {date.Display}", true, "date");
        }

        warnings.Add("Circular letter title uses a generic label because no reliable date was found.");
        return new TitleDetection("Circular Letter", false, "generic");
    }

    if (category is not ("Book" or "Brochure"))
    {
        warnings.Add("Official title could not be detected because the category is unknown.");
        return new TitleDetection(string.Empty, false, "unknown");
    }

    var knownTitle = TryMapKnownTitle(fileName);
    if (!string.IsNullOrWhiteSpace(knownTitle))
    {
        var matchedInText = TextContainsTitle(firstPagesText, knownTitle);
        if (!matchedInText && !string.IsNullOrWhiteSpace(firstPagesText))
        {
            warnings.Add("Title was taken from the filename; extracted first pages did not clearly repeat it.");
        }

        return new TitleDetection(knownTitle, true, matchedInText ? "text-and-filename" : "filename");
    }

    var titleFromText = ExtractLikelyTitleFromText(firstPagesText);
    if (!string.IsNullOrWhiteSpace(titleFromText))
    {
        return new TitleDetection(titleFromText, true, "text");
    }

    warnings.Add("Official title could not be detected with confidence.");
    return new TitleDetection(string.Empty, false, "unknown");
}

static string? TryMapKnownTitle(string fileName)
{
    var stem = NormalizeForMatching(Path.GetFileNameWithoutExtension(fileName));
    var cleaned = stem
        .Replace("en ef ", string.Empty, StringComparison.Ordinal)
        .Replace("ef ", string.Empty, StringComparison.Ordinal)
        .Trim();

    var knownTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["baptism lord s supper and footwashing"] = "Baptism, Lord's Supper, Footwashing",
        ["god and his plan with humanity"] = "God and His Plan with Humanity",
        ["return of christ"] = "The Return of Christ",
        ["the bible the most read book on earth"] = "The Bible - the Most Read Book on Earth",
        ["traditional christianity"] = "Traditional Christianity",
        ["global information"] = "Global Information",
        ["pakistan february 2010"] = "Pakistan February 2010",
        ["people ask questions"] = "People Ask Questions, God Answers by His Word",
        ["sixty years in the service of the lord"] = "Sixty Years in the Service of the LORD",
        ["the revelation a book with 7 seals"] = "The Revelation - A Book with 7 Seals",
        ["to the bride church of jesus christ"] = "To the Bride Church of Jesus Christ",
        ["vision 7000"] = "Vision 7000"
    };

    return knownTitles.TryGetValue(cleaned, out var title) ? title : null;
}

static string ExtractLikelyTitleFromText(string firstPagesText)
{
    var rejectPatterns = new[]
    {
        "ewald frank",
        "mission center",
        "postfach",
        "http",
        "www.",
        "email",
        "page ",
        "copyright",
        "published by"
    };

    foreach (var rawLine in firstPagesText.Split('\n').Select(line => line.Trim()))
    {
        var line = Regex.Replace(rawLine, @"\s+", " ");
        if (line.Length is < 8 or > 90)
        {
            continue;
        }

        var normalized = NormalizeForMatching(line);
        if (rejectPatterns.Any(pattern => normalized.Contains(pattern, StringComparison.Ordinal)) ||
            Regex.IsMatch(normalized, @"^\d+$"))
        {
            continue;
        }

        if (line.Count(char.IsLetter) < 6)
        {
            continue;
        }

        return ToDisplayTitle(line);
    }

    return string.Empty;
}

static string CreateTopicNote(string category, string title, string firstPagesText)
{
    var normalizedTitle = NormalizeForMatching(title);
    var normalizedText = NormalizeForMatching(firstPagesText);
    var combined = $"{normalizedTitle} {normalizedText}";

    if (category == "Circular Letter")
    {
        return "Circular letter with greetings, missionary updates, and Scriptural exhortation.";
    }

    if (combined.Contains("baptism", StringComparison.Ordinal) ||
        combined.Contains("footwashing", StringComparison.Ordinal))
    {
        return "Teaching brochure about baptism, Lord's Supper, and footwashing.";
    }

    if (combined.Contains("return of christ", StringComparison.Ordinal))
    {
        return "Booklet about the return of Christ.";
    }

    if (combined.Contains("plan with humanity", StringComparison.Ordinal))
    {
        return "Booklet about God's plan with humanity.";
    }

    if (combined.Contains("most read book", StringComparison.Ordinal))
    {
        return "Booklet about the Bible and its place as the most read book on earth.";
    }

    if (combined.Contains("bride church", StringComparison.Ordinal))
    {
        return "Brochure addressed to the Bride Church of Jesus Christ.";
    }

    if (combined.Contains("people ask questions", StringComparison.Ordinal))
    {
        return "Question-and-answer booklet using Scripture to address common faith questions.";
    }

    if (combined.Contains("traditional christianity", StringComparison.Ordinal))
    {
        return "Booklet comparing traditional Christianity with Scriptural teaching.";
    }

    if (combined.Contains("revelation", StringComparison.Ordinal) ||
        combined.Contains("seven seals", StringComparison.Ordinal))
    {
        return "Booklet about the book of Revelation and the seven seals.";
    }

    if (combined.Contains("sixty years", StringComparison.Ordinal))
    {
        return "Short autobiographical booklet about ministry service.";
    }

    if (combined.Contains("vision 7000", StringComparison.Ordinal))
    {
        return "Brochure about international mission outreach and broadcast vision.";
    }

    if (combined.Contains("global information", StringComparison.Ordinal))
    {
        return "Brochure with global contact and ministry information.";
    }

    if (combined.Contains("pakistan february 2010", StringComparison.Ordinal))
    {
        return "Brochure or report about Brother Frank's Pakistan visit in February 2010.";
    }

    return category switch
    {
        "Book" => "Booklet or book with Biblical teaching by Brother Ewald Frank.",
        "Brochure" => "Brochure with Biblical teaching or ministry information by Brother Ewald Frank.",
        _ => "Topic note could not be determined confidently from the available text."
    };
}

static string CreateSuggestedFileName(string category, string title, DateDetection date, string originalFileName)
{
    var originalStem = Path.GetFileNameWithoutExtension(originalFileName);
    var fallbackStem = SafeFileToken(originalStem, maxLength: 90);

    var fileName = category switch
    {
        "Circular Letter" when date.FilePrefix.EndsWith("Year-End", StringComparison.Ordinal) =>
            $"EF-Circular-Letter-{date.FilePrefix}-English.pdf",
        "Circular Letter" when Regex.IsMatch(date.FilePrefix, @"\d{4}-(Spring|Summer|Autumn|Fall|Winter)$", RegexOptions.IgnoreCase) =>
            $"EF-Circular-Letter-{date.FilePrefix}-English.pdf",
        "Circular Letter" when date.IsConfident =>
            $"EF-Circular-Letter-{date.FilePrefix}-{SafeFileToken(date.Display, 70)}-English.pdf",
        "Circular Letter" =>
            $"EF-Circular-Letter-Undated-{fallbackStem}-English.pdf",
        "Book" when !string.IsNullOrWhiteSpace(title) =>
            $"EF-Book-{SafeFileToken(title, 115)}-English.pdf",
        "Brochure" when !string.IsNullOrWhiteSpace(title) =>
            $"EF-Brochure-{SafeFileToken(title, 110)}-English.pdf",
        "Book" or "Brochure" =>
            $"EF-{category}-{fallbackStem}-English.pdf",
        _ => $"EF-Unknown-{fallbackStem}-English.pdf"
    };

    return LimitFileName(fileName, 150);
}

static string DetermineConfidence(
    bool extractionSucceeded,
    string category,
    string language,
    DateDetection date,
    TitleDetection titleDetection,
    IReadOnlyCollection<string> warnings)
{
    if (!extractionSucceeded ||
        category == "Unknown" ||
        language != "English" ||
        (category == "Circular Letter" && !date.IsConfident) ||
        !titleDetection.IsConfident)
    {
        return "Low";
    }

    if (warnings.Count > 0 || titleDetection.Source == "filename")
    {
        return "Medium";
    }

    return "High";
}

static ValidationSummary Validate(IReadOnlyList<PdfMetadataRecord> records)
{
    var issues = new List<string>();

    foreach (var record in records)
    {
        if (string.IsNullOrWhiteSpace(record.DetectedCategory))
        {
            issues.Add($"{record.OriginalFile}: missing category.");
        }

        if (string.IsNullOrWhiteSpace(record.SuggestedFileName))
        {
            issues.Add($"{record.OriginalFile}: missing suggested filename.");
        }

        if (record.DetectedCategory == "Circular Letter" &&
            string.IsNullOrWhiteSpace(record.DetectedDate))
        {
            issues.Add($"{record.OriginalFile}: circular letter date is not visible.");
        }
    }

    return new ValidationSummary(issues);
}

static void WritePreviewCsv(IEnumerable<PdfMetadataRecord> records, string path)
{
    var builder = new StringBuilder();
    builder.AppendLine("original_file,current_folder,detected_category,detected_language,detected_date,official_display_title,topic_note,suggested_filename,suggested_destination,confidence,warnings");
    foreach (var record in records)
    {
        builder.AppendLine(string.Join(
            ',',
            Csv(record.OriginalFile),
            Csv(record.CurrentFolder),
            Csv(record.DetectedCategory),
            Csv(record.DetectedLanguage),
            Csv(record.DetectedDate),
            Csv(record.OfficialDisplayTitle),
            Csv(record.TopicNote),
            Csv(record.SuggestedFileName),
            Csv(record.SuggestedDestination),
            Csv(record.Confidence),
            Csv(record.Warnings)));
    }

    File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
}

static void WriteAppliedCsv(IEnumerable<AppliedRecord> records, string path)
{
    var builder = new StringBuilder();
    builder.AppendLine("original_file,current_folder,detected_category,detected_language,detected_date,official_display_title,topic_note,suggested_filename,suggested_destination,applied_file,confidence,warnings");
    foreach (var record in records)
    {
        builder.AppendLine(string.Join(
            ',',
            Csv(record.Metadata.OriginalFile),
            Csv(record.Metadata.CurrentFolder),
            Csv(record.Metadata.DetectedCategory),
            Csv(record.Metadata.DetectedLanguage),
            Csv(record.Metadata.DetectedDate),
            Csv(record.Metadata.OfficialDisplayTitle),
            Csv(record.Metadata.TopicNote),
            Csv(record.Metadata.SuggestedFileName),
            Csv(record.Metadata.SuggestedDestination),
            Csv(record.AppliedFile),
            Csv(record.Metadata.Confidence),
            Csv(record.Metadata.Warnings)));
    }

    File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
}

static void WriteTextReport(
    IReadOnlyList<PdfMetadataRecord> records,
    ValidationSummary validation,
    string path,
    bool apply)
{
    var circularCount = records.Count(record => record.DetectedCategory == "Circular Letter");
    var bookBrochureCount = records.Count(record => record.DetectedCategory is "Book" or "Brochure");
    var unknownCount = records.Count(record => record.DetectedCategory == "Unknown");
    var lowConfidenceCount = records.Count(record => record.Confidence == "Low");

    var builder = new StringBuilder();
    builder.AppendLine("Brother Frank PDF Metadata Preview");
    builder.AppendLine();
    builder.AppendLine($"Mode: {(apply ? "Apply copy only" : "Dry run preview only")}");
    builder.AppendLine($"Total PDFs scanned: {records.Count:N0}");
    builder.AppendLine($"Circular letters found: {circularCount:N0}");
    builder.AppendLine($"Books/brochures found: {bookBrochureCount:N0}");
    builder.AppendLine($"Unknown files: {unknownCount:N0}");
    builder.AppendLine($"Files with low confidence: {lowConfidenceCount:N0}");
    builder.AppendLine();
    builder.AppendLine($"Preview CSV: {previewCsvPath}");
    builder.AppendLine($"Preview report: {previewTextPath}");
    builder.AppendLine();
    builder.AppendLine("Validation");
    if (validation.Issues.Count == 0)
    {
        builder.AppendLine("- PASS: every PDF has a category or Unknown.");
        builder.AppendLine("- PASS: every PDF has a suggested filename.");
        builder.AppendLine("- PASS: every circular letter with a confident date keeps that date visible.");
        builder.AppendLine("- PASS: no original file is deleted by this tool.");
        builder.AppendLine("- PASS: no MessageFlow database is opened or modified by this tool.");
    }
    else
    {
        foreach (var issue in validation.Issues)
        {
            builder.AppendLine($"- REVIEW: {issue}");
        }
    }

    builder.AppendLine();
    builder.AppendLine("Suggested next action");
    if (lowConfidenceCount > 0 || unknownCount > 0 || validation.Issues.Count > 0)
    {
        builder.AppendLine("Review the preview CSV before copying files. Pay closest attention to Low confidence and Unknown rows.");
    }
    else
    {
        builder.AppendLine("Review the preview CSV. If it looks correct, run the tool again with --apply to copy files into the organized folders.");
    }

    File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
}

static AppliedRecord[] ApplyCopies(IEnumerable<PdfMetadataRecord> records)
{
    var applied = new List<AppliedRecord>();

    foreach (var record in records)
    {
        Directory.CreateDirectory(record.SuggestedDestination);
        var destinationPath = GetAvailableDestinationPath(record.SuggestedDestination, record.SuggestedFileName);
        File.Copy(record.SourceFilePath, destinationPath, overwrite: false);
        applied.Add(new AppliedRecord(record, destinationPath));
    }

    return applied.ToArray();
}

static string GetAvailableDestinationPath(string destinationFolder, string suggestedFileName)
{
    var destinationPath = Path.Combine(destinationFolder, suggestedFileName);
    if (!File.Exists(destinationPath))
    {
        return destinationPath;
    }

    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(suggestedFileName);
    var extension = Path.GetExtension(suggestedFileName);
    for (var suffix = 2; suffix < 10_000; suffix++)
    {
        destinationPath = Path.Combine(destinationFolder, $"{fileNameWithoutExtension}-{suffix}{extension}");
        if (!File.Exists(destinationPath))
        {
            return destinationPath;
        }
    }

    throw new InvalidOperationException($"Could not find an available filename for {suggestedFileName}.");
}

static void PrintSummary(
    IReadOnlyList<PdfMetadataRecord> records,
    ValidationSummary validation,
    bool apply,
    int appliedCount)
{
    Console.WriteLine();
    Console.WriteLine("Summary");
    Console.WriteLine($"Total PDFs scanned: {records.Count:N0}");
    Console.WriteLine($"Circular letters found: {records.Count(record => record.DetectedCategory == "Circular Letter"):N0}");
    Console.WriteLine($"Books/brochures found: {records.Count(record => record.DetectedCategory is "Book" or "Brochure"):N0}");
    Console.WriteLine($"Unknown files: {records.Count(record => record.DetectedCategory == "Unknown"):N0}");
    Console.WriteLine($"Low confidence files: {records.Count(record => record.Confidence == "Low"):N0}");
    Console.WriteLine($"Preview CSV: {previewCsvPath}");
    Console.WriteLine($"Preview report: {previewTextPath}");
    if (apply)
    {
        Console.WriteLine($"Copied files: {appliedCount:N0}");
        Console.WriteLine($"Applied CSV: {appliedCsvPath}");
    }
    else
    {
        Console.WriteLine(@"Apply later with: dotnet run --project tools\OrganizeEwaldFrankPdfs -- --apply");
    }

    if (validation.Issues.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Validation review items:");
        foreach (var issue in validation.Issues.Take(10))
        {
            Console.WriteLine($"- {issue}");
        }

        if (validation.Issues.Count > 10)
        {
            Console.WriteLine($"- plus {validation.Issues.Count - 10:N0} more");
        }
    }
}

static bool TextContainsTitle(string firstPagesText, string title)
{
    if (string.IsNullOrWhiteSpace(firstPagesText))
    {
        return false;
    }

    return NormalizeForMatching(firstPagesText).Contains(NormalizeForMatching(title), StringComparison.Ordinal);
}

static string ToDisplayTitle(string value)
{
    var trimmed = Regex.Replace(value.Trim(), @"\s+", " ");
    if (trimmed.Any(char.IsLower))
    {
        return trimmed;
    }

    return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
}

static string SafeFileToken(string value, int maxLength)
{
    var normalized = value.Normalize(NormalizationForm.FormD);
    var builder = new StringBuilder(normalized.Length);
    foreach (var character in normalized)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(character);
        if (category == UnicodeCategory.NonSpacingMark)
        {
            continue;
        }

        if (char.IsLetterOrDigit(character))
        {
            builder.Append(character);
            continue;
        }

        if (character is '&')
        {
            builder.Append(" and ");
            continue;
        }

        if (character is '-' or '_' or ' ')
        {
            builder.Append(' ');
        }
    }

    var cleaned = Regex.Replace(builder.ToString(), @"\s+", "-").Trim('-');
    if (string.IsNullOrWhiteSpace(cleaned))
    {
        cleaned = "Untitled";
    }

    return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength].Trim('-');
}

static string LimitFileName(string fileName, int maxLength)
{
    if (fileName.Length <= maxLength)
    {
        return fileName;
    }

    var extension = Path.GetExtension(fileName);
    var stem = Path.GetFileNameWithoutExtension(fileName);
    var allowedStemLength = Math.Max(12, maxLength - extension.Length);
    return $"{stem[..allowedStemLength].Trim('-')}{extension}";
}

static string NormalizeForMatching(string value)
{
    var normalized = NormalizeDashes(value).Normalize(NormalizationForm.FormD);
    var builder = new StringBuilder(normalized.Length);
    foreach (var character in normalized)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(character);
        if (category == UnicodeCategory.NonSpacingMark)
        {
            continue;
        }

        builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
    }

    return $" {Regex.Replace(builder.ToString(), @"\s+", " ").Trim()} ";
}

static string NormalizeDashes(string value)
{
    return value
        .Replace('\u2010', '-')
        .Replace('\u2011', '-')
        .Replace('\u2012', '-')
        .Replace('\u2013', '-')
        .Replace('\u2014', '-')
        .Replace('\u2212', '-');
}

static string Csv(string? value)
{
    value ??= string.Empty;
    return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}

static bool TryGetMonth(string value, out MonthInfo month)
{
    var key = value.Trim().ToLowerInvariant();
    var abbreviated = key.Length > 3 ? key[..3] : key;

    month = abbreviated switch
    {
        "jan" => new MonthInfo(1, "January"),
        "feb" => new MonthInfo(2, "February"),
        "mar" => new MonthInfo(3, "March"),
        "apr" => new MonthInfo(4, "April"),
        "may" => new MonthInfo(5, "May"),
        "jun" => new MonthInfo(6, "June"),
        "jul" => new MonthInfo(7, "July"),
        "aug" => new MonthInfo(8, "August"),
        "sep" => new MonthInfo(9, "September"),
        "oct" => new MonthInfo(10, "October"),
        "nov" => new MonthInfo(11, "November"),
        "dec" => new MonthInfo(12, "December"),
        _ => new MonthInfo(0, string.Empty)
    };

    return month.Number > 0;
}

static bool TryGetMonthByNumber(int number, out MonthInfo month)
{
    month = number switch
    {
        1 => new MonthInfo(1, "January"),
        2 => new MonthInfo(2, "February"),
        3 => new MonthInfo(3, "March"),
        4 => new MonthInfo(4, "April"),
        5 => new MonthInfo(5, "May"),
        6 => new MonthInfo(6, "June"),
        7 => new MonthInfo(7, "July"),
        8 => new MonthInfo(8, "August"),
        9 => new MonthInfo(9, "September"),
        10 => new MonthInfo(10, "October"),
        11 => new MonthInfo(11, "November"),
        12 => new MonthInfo(12, "December"),
        _ => new MonthInfo(0, string.Empty)
    };

    return month.Number > 0;
}

sealed record MonthInfo(int Number, string Name);

sealed record DateDetection(string Display, string FilePrefix, bool IsConfident)
{
    public static DateDetection None { get; } = new(string.Empty, "Undated", false);
}

sealed record TitleDetection(string Title, bool IsConfident, string Source);

sealed record PdfMetadataRecord(
    string SourceFilePath,
    string OriginalFile,
    string CurrentFolder,
    string DetectedCategory,
    string DetectedLanguage,
    string DetectedDate,
    string OfficialDisplayTitle,
    string TopicNote,
    string SuggestedFileName,
    string SuggestedDestination,
    string Confidence,
    string Warnings);

sealed record AppliedRecord(PdfMetadataRecord Metadata, string AppliedFile);

sealed record ValidationSummary(IReadOnlyList<string> Issues)
{
    public bool HasBlockingFailures => Issues.Count > 0;
}
