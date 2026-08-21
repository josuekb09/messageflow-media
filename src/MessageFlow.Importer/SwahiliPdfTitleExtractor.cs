using System.Globalization;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace MessageFlow.Importer;

public static partial class SwahiliPdfTitleExtractor
{
    private const int MaxTitleLength = 300;

    public static bool TryExtractFromPdf(string filePath, out string title)
    {
        title = string.Empty;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        try
        {
            using var document = PdfDocument.Open(filePath);
            var page = document.GetPages().FirstOrDefault();
            if (page is null)
            {
                return false;
            }

            var words = page.GetWords()
                .Where(word => !string.IsNullOrWhiteSpace(word.Text))
                .Select(word => new CoverWord(
                    NormalizeToken(word.Text),
                    word.BoundingBox.Height,
                    word.BoundingBox.Top,
                    word.BoundingBox.Left,
                    word.Letters.Count))
                .Where(word => word.Text.Length > 0 && !IsFurniture(word.Text))
                .ToList();

            if (words.Count == 0)
            {
                return false;
            }

            var heights = words.Select(word => word.Height).OrderBy(height => height).ToList();
            var bodyHeight = heights[heights.Count / 2];
            var titleWords = SelectTitleBand(words, bodyHeight);
            if (titleWords.Count == 0)
            {
                return false;
            }

            var lines = GroupLines(titleWords, 14);
            var rebuiltLines = new List<string>();
            foreach (var line in lines)
            {
                if (IsBodyLine(line, bodyHeight))
                {
                    break;
                }

                var rebuilt = RebuildLine(line);
                if (string.IsNullOrWhiteSpace(rebuilt) || LooksLikeSpokenBody(rebuilt))
                {
                    break;
                }

                rebuiltLines.Add(rebuilt);
                if (rebuiltLines.Count >= 6)
                {
                    break;
                }
            }

            var raw = CollapseSpaces(string.Join(" ", rebuiltLines));
            raw = MergeLoneLetters(raw);
            raw = FixKnownGluedParticles(raw);
            raw = CollapseSpaces(HyphenSpacingRegex().Replace(raw, "-"));
            raw = CommaSpacingRegex().Replace(raw, ", ");
            raw = NormalizeParentheses(raw);
            raw = ToSwahiliTitleCase(raw);

            if (!IsPlausibleTitle(raw))
            {
                return false;
            }

            title = raw.Length <= MaxTitleLength ? raw : raw[..MaxTitleLength].Trim();
            return true;
        }
        catch (Exception)
        {
            title = string.Empty;
            return false;
        }
    }

    private static List<CoverWord> SelectTitleBand(IReadOnlyList<CoverWord> words, double bodyHeight)
    {
        var lines = GroupLines(words, Math.Max(8, bodyHeight * 0.85));
        var titleWords = new List<CoverWord>();
        foreach (var line in lines)
        {
            if (IsBodyLine(line, bodyHeight) && titleWords.Count > 0)
            {
                break;
            }

            if (IsBodyLine(line, bodyHeight) && titleWords.Count == 0)
            {
                continue;
            }

            titleWords.AddRange(line);
            if (titleWords.Count > 40)
            {
                break;
            }
        }

        return titleWords;
    }

    private static bool IsBodyLine(IReadOnlyList<CoverWord> line, double bodyHeight)
    {
        if (line.Count == 0)
        {
            return false;
        }

        var maxHeight = line.Max(word => word.Height);
        var averageHeight = line.Average(word => word.Height);
        if (LooksLikeSpokenBody(string.Join(" ", line.Select(word => word.Text))))
        {
            return true;
        }

        return line.Count >= 4 &&
               maxHeight <= bodyHeight * 1.18 &&
               averageHeight <= bodyHeight * 1.12;
    }

    private static List<List<CoverWord>> GroupLines(IReadOnlyList<CoverWord> words, double tolerance)
    {
        var lines = new List<List<CoverWord>>();
        foreach (var word in words.OrderByDescending(item => item.Top).ThenBy(item => item.Left))
        {
            var line = lines.FirstOrDefault(existing => Math.Abs(existing[0].Top - word.Top) <= tolerance);
            if (line is null)
            {
                lines.Add([word]);
            }
            else
            {
                line.Add(word);
            }
        }

        return lines;
    }

    private static string RebuildLine(IReadOnlyList<CoverWord> line)
    {
        var ordered = line.OrderBy(word => word.Left).ToList();
        if (ordered.Count == 0)
        {
            return string.Empty;
        }

        var remainderHeight = ordered.Min(word => word.Height);
        var maxHeight = ordered.Max(word => word.Height);
        var initials = ordered
            .Where(word => IsDropCapInitial(word, remainderHeight, maxHeight))
            .ToList();
        var remainders = ordered.Except(initials).ToList();

        if (initials.Count == 0 || remainders.Count == 0)
        {
            return string.Join(" ", ordered.Select(word => word.Text));
        }

        var pieces = new List<(double Left, string Text)>();
        var usedRemainders = new HashSet<CoverWord>();

        for (var index = 0; index < initials.Count; index++)
        {
            var initial = initials[index];
            var nextLeft = index + 1 < initials.Count ? initials[index + 1].Left : double.MaxValue;
            var remainder = remainders
                .Where(word => !usedRemainders.Contains(word))
                .Where(word => word.Left > initial.Left - 1 && word.Left < nextLeft - 3)
                .OrderBy(word => word.Left)
                .Select(word => (CoverWord?)word)
                .FirstOrDefault();

            var punctuation = string.Concat(initial.Text.TakeWhile(character => !char.IsLetter(character)));
            var letter = string.Concat(initial.Text.SkipWhile(character => !char.IsLetter(character)));
            var stem = remainder?.Text ?? string.Empty;
            if (remainder is { } matchedRemainder)
            {
                usedRemainders.Add(matchedRemainder);
            }

            var wordText = letter + stem;
            if (punctuation.Length > 0 && pieces.Count > 0)
            {
                var previous = pieces[^1];
                pieces[^1] = (previous.Left, previous.Text + punctuation);
                pieces.Add((initial.Left, wordText));
            }
            else
            {
                pieces.Add((initial.Left, punctuation + wordText));
            }
        }

        foreach (var remainder in remainders.Where(word => !usedRemainders.Contains(word)))
        {
            pieces.Add((remainder.Left, remainder.Text));
        }

        return string.Join(" ", pieces.OrderBy(piece => piece.Left).Select(piece => piece.Text));
    }

    private static bool IsDropCapInitial(CoverWord word, double remainderHeight, double maxHeight)
    {
        var letters = word.Text.Count(char.IsLetter);
        if (letters is < 1 or > 2)
        {
            return false;
        }

        return word.Height >= remainderHeight + 2.0 &&
               word.Height >= maxHeight * 0.75;
    }

    private static bool LooksLikeSpokenBody(string value)
    {
        var trimmed = value.TrimStart('.', '?', '…', ' ', '…');
        return trimmed.StartsWith("Ndugu", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Asante", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Habari", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Ninataka", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Sijui", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Sasa,", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Nina ", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Nasi ", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Mwaweza", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Na,", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Bwana awabariki", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("…?…", StringComparison.Ordinal);
    }

    private static bool IsFurniture(string value)
    {
        if (value.Length == 1 &&
            !char.IsLetterOrDigit(value[0]) &&
            value[0] is not '(' and not ')')
        {
            return true;
        }

        var normalized = NonLetterDigitRegex().Replace(value, string.Empty).ToUpperInvariant();
        return normalized is "THESPOKENWORD" or "VOICEOFGODRECORDINGS" or "VGR" or "KISWAHILI" or "SWAHILI" ||
               SermonCodePrefixRegex().IsMatch(value) ||
               PageNumberRegex().IsMatch(value);
    }

    private static bool IsPlausibleTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 3 || value.Length > 220)
        {
            return false;
        }

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length is 0 or > 18)
        {
            return false;
        }

        if (words.Count(word =>
                LettersOnly(word).Length == 1 &&
                !IsAllowedSingleLetter(LettersOnly(word))) > 0)
        {
            return false;
        }

        var englishHits = CountMarkers(words, EnglishMarkers);
        var swahiliHits = CountMarkers(words, SwahiliMarkers);
        if (englishHits >= 2 && englishHits > swahiliHits)
        {
            return false;
        }

        return words.All(word => LettersOnly(word).Length > 0);
    }

    private static int CountMarkers(IReadOnlyList<string> words, HashSet<string> markers)
    {
        return words.Count(word => markers.Contains(LettersOnly(word)));
    }

    private static string FixKnownGluedParticles(string value)
    {
        value = GluedMstariWaRegex().Replace(value, "MSTARI WA");
        value = GluedMwandikoWaRegex().Replace(value, "MWANDIKO WA");
        value = GluedAlamaYaRegex().Replace(value, "ALAMA YA");
        value = GluedMuhuriWaRegex().Replace(value, "MUHURI WA");
        value = GluedAganoLaRegex().Replace(value, "AGANO LA");
        return value;
    }

    private static string MergeLoneLetters(string value)
    {
        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var merged = new List<string>();
        for (var index = 0; index < tokens.Count; index++)
        {
            var current = tokens[index];
            if (index + 1 < tokens.Count &&
                LettersOnly(current).Length == 1 &&
                LettersOnly(tokens[index + 1]).Length >= 3 &&
                !SwahiliMarkers.Contains(LettersOnly(tokens[index + 1])))
            {
                var punctuation = string.Concat(current.TakeWhile(character => !char.IsLetter(character)));
                var letter = string.Concat(current.SkipWhile(character => !char.IsLetter(character)));
                merged.Add(punctuation + letter + tokens[index + 1]);
                index++;
                continue;
            }

            merged.Add(current);
        }

        return string.Join(" ", merged);
    }

    private static string ToSwahiliTitleCase(string value)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < words.Length; index++)
        {
            var word = words[index];
            var leading = string.Concat(word.TakeWhile(character => !char.IsLetterOrDigit(character)));
            var trailing = string.Concat(word.Reverse().TakeWhile(character => !char.IsLetterOrDigit(character)).Reverse());
            var coreLength = word.Length - leading.Length - trailing.Length;
            if (coreLength <= 0)
            {
                continue;
            }

            var core = word.Substring(leading.Length, coreLength);
            words[index] = leading + TitleCaseCore(core, isFirstWord: index == 0) + trailing;
        }

        return string.Join(" ", words);
    }

    private static string TitleCaseCore(string core, bool isFirstWord)
    {
        if (core.Contains('-', StringComparison.Ordinal))
        {
            return string.Join('-', core.Split('-').Select((part, index) => TitleCasePart(part, isFirstWord && index == 0)));
        }

        return TitleCasePart(core, isFirstWord);
    }

    private static string TitleCasePart(string value, bool isFirstWord)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var lower = value.ToLowerInvariant();
        if (!isFirstWord && SwahiliMarkers.Contains(lower))
        {
            return lower;
        }

        return char.ToUpper(lower[0], CultureInfo.InvariantCulture) + lower[1..];
    }

    private static string NormalizeParentheses(string value)
    {
        value = MissingSpaceBeforeParenRegex().Replace(value, " (");
        value = value.Replace("( ", "(", StringComparison.Ordinal).Replace(" )", ")", StringComparison.Ordinal);
        var opens = value.Count(character => character == '(');
        var closes = value.Count(character => character == ')');
        if (opens == closes + 1)
        {
            value += ")";
        }

        return CollapseSpaces(value);
    }

    private static bool IsAllowedSingleLetter(string value)
    {
        return value.Equals("I", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("V", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("X", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeToken(string value)
    {
        return value.Replace("…", "...", StringComparison.Ordinal).Trim();
    }

    private static string CollapseSpaces(string value) => WhiteSpaceRegex().Replace(value, " ").Trim();

    private static string LettersOnly(string value) => NonLetterDigitRegex().Replace(value, string.Empty);

    private static readonly HashSet<string> SwahiliMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "wa", "ya", "za", "na", "la", "cha", "kwa", "katika", "ni"
    };

    private static readonly HashSet<string> EnglishMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "The", "And", "Of", "For", "To", "In", "On", "With", "From", "That", "This", "Is", "Are"
    };

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();

    [GeneratedRegex(@"[^A-Za-z0-9]+")]
    private static partial Regex NonLetterDigitRegex();

    [GeneratedRegex(@"^SWA\d", RegexOptions.IgnoreCase)]
    private static partial Regex SermonCodePrefixRegex();

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex PageNumberRegex();

    [GeneratedRegex(@"\s*-\s*")]
    private static partial Regex HyphenSpacingRegex();

    [GeneratedRegex(@"\s*,\s*")]
    private static partial Regex CommaSpacingRegex();

    [GeneratedRegex(@"(?<=\p{L})\(")]
    private static partial Regex MissingSpaceBeforeParenRegex();

    [GeneratedRegex(@"\bMSTARIWA\b", RegexOptions.IgnoreCase)]
    private static partial Regex GluedMstariWaRegex();

    [GeneratedRegex(@"\bMWANDIKOWA\b", RegexOptions.IgnoreCase)]
    private static partial Regex GluedMwandikoWaRegex();

    [GeneratedRegex(@"\bALAMAYA\b", RegexOptions.IgnoreCase)]
    private static partial Regex GluedAlamaYaRegex();

    [GeneratedRegex(@"\bMUHURIWA\b", RegexOptions.IgnoreCase)]
    private static partial Regex GluedMuhuriWaRegex();

    [GeneratedRegex(@"\bAGANOLA\b", RegexOptions.IgnoreCase)]
    private static partial Regex GluedAganoLaRegex();

    private readonly record struct CoverWord(string Text, double Height, double Top, double Left, int LetterCount);
}
