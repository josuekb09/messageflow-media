using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MessageFlow.Importer;

public static partial class FrenchPdfTitleExtractor
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
                .SelectMany(SplitWordByLetterGaps)
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
            raw = NormalizeFrenchApostrophes(raw);
            raw = CollapseSpaces(HyphenSpacingRegex().Replace(raw, "-"));
            raw = CommaSpacingRegex().Replace(raw, ", ");
            raw = NormalizeParentheses(raw);
            raw = ToFrenchTitleCase(raw);
            raw = SplitGluedFrenchA(raw);
            raw = ToFrenchTitleCase(raw);

            if (!IsPlausibleTitle(raw) && TryFallbackFromFileName(filePath, out var fallbackTitle))
            {
                raw = fallbackTitle;
            }

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

    private static IEnumerable<CoverWord> SplitWordByLetterGaps(Word word)
    {
        var letters = word.Letters
            .Where(letter => !string.IsNullOrEmpty(letter.Value))
            .OrderBy(letter => letter.GlyphRectangle.Left)
            .ToList();

        if (letters.Count == 0)
        {
            yield break;
        }

        if (letters.Count == 1)
        {
            var single = NormalizeToken(letters[0].Value);
            if (single.Length > 0)
            {
                yield return new CoverWord(
                    single,
                    word.BoundingBox.Height,
                    word.BoundingBox.Top,
                    word.BoundingBox.Left,
                    1);
            }

            yield break;
        }

        var widths = letters
            .Select(letter => letter.GlyphRectangle.Width)
            .Where(width => width > 0)
            .Order()
            .ToList();
        var medianWidth = widths.Count == 0 ? 8 : widths[widths.Count / 2];
        var gapThreshold = Math.Max(2.35, medianWidth * 0.28);

        var current = new StringBuilder();
        var startLeft = letters[0].GlyphRectangle.Left;
        Letter? previous = null;
        foreach (var letter in letters)
        {
            if (previous is not null &&
                letter.GlyphRectangle.Left - previous.GlyphRectangle.Right > gapThreshold &&
                current.Length > 0)
            {
                yield return new CoverWord(
                    NormalizeToken(current.ToString()),
                    word.BoundingBox.Height,
                    word.BoundingBox.Top,
                    startLeft,
                    current.Length);
                current.Clear();
                startLeft = letter.GlyphRectangle.Left;
            }

            current.Append(letter.Value);
            previous = letter;
        }

        if (current.Length > 0)
        {
            yield return new CoverWord(
                NormalizeToken(current.ToString()),
                word.BoundingBox.Height,
                word.BoundingBox.Top,
                startLeft,
                current.Length);
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
            if (titleWords.Count > 48)
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
        var trimmed = value.TrimStart('.', '?', '…', ' ', '(', '…');
        return trimmed.StartsWith("Frère", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Frere", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Merci", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Bonjour", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Bonsoir", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("C’est", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("C'est", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Je veux", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Je suis", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("Quand ", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("…le ", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("...le ", StringComparison.Ordinal) ||
               trimmed.Contains("…?…", StringComparison.Ordinal);
    }

    private static bool IsFurniture(string value)
    {
        if (value.Length == 1 &&
            !char.IsLetterOrDigit(value[0]) &&
            value[0] is not '(' and not ')' and not ',' and not '-' and not '\'' and not '\u2019')
        {
            return true;
        }

        var normalized = NonLetterDigitRegex().Replace(value, string.Empty).ToUpperInvariant();
        return normalized is "THESPOKENWORD" or "VOICEOFGODRECORDINGS" or "VGR" or "FRANCAIS" or "FRANÇAIS" or "FRENCH" ||
               SermonCodePrefixRegex().IsMatch(value) ||
               PageNumberRegex().IsMatch(value);
    }

    private static bool IsPlausibleTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 3 || value.Length > 240)
        {
            return false;
        }

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length is 0 or > 30)
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
        var frenchHits = CountMarkers(words, FrenchMarkers);
        if (englishHits >= 2 && englishHits > frenchHits)
        {
            return false;
        }

        return words.All(word => LettersOnly(word).Length > 0 || word.Any(character => character is '\'' or '\u2019'));
    }

    private static int CountMarkers(IReadOnlyList<string> words, HashSet<string> markers)
    {
        return words.Count(word => markers.Contains(LettersOnly(word)));
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
                !FrenchMarkers.Contains(LettersOnly(tokens[index + 1])))
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

    private static string ToFrenchTitleCase(string value)
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
        if (!isFirstWord && FrenchMarkers.Contains(LettersOnly(lower)))
        {
            return lower;
        }

        return char.ToUpper(lower[0], CultureInfo.InvariantCulture) + lower[1..];
    }

    private static string SplitGluedFrenchA(string value)
    {
        return GluedFrenchARegex().Replace(value, "A $1");
    }

    private static bool TryFallbackFromFileName(string filePath, out string title)
    {
        title = string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        fileName = Regex.Replace(fileName, @"^(?:FRN|FRA|FRE|FR)", string.Empty, RegexOptions.IgnoreCase);
        fileName = Regex.Replace(fileName, @"\bVGR\b", string.Empty, RegexOptions.IgnoreCase);
        fileName = Regex.Replace(fileName, @"^\s*\d{2}-\d{4}[A-Za-z]?\s*", string.Empty);
        var english = CollapseSpaces(fileName);
        if (!OfficialFrenchTitleFallbacks.TryGetValue(english, out var french))
        {
            return false;
        }

        title = french;
        return true;
    }

    private static string NormalizeFrenchApostrophes(string value)
    {
        value = LooseApostropheRegex().Replace(value, "'");
        value = ElidedPrefixRegex().Replace(value, "$1'");
        return CollapseSpaces(value);
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
               value.Equals("A", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Y", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeToken(string value)
    {
        return value.Replace("…", "...", StringComparison.Ordinal).Trim();
    }

    private static string CollapseSpaces(string value) => WhiteSpaceRegex().Replace(value, " ").Trim();

    private static string LettersOnly(string value) => NonLetterDigitRegex().Replace(value, string.Empty);

    private static readonly HashSet<string> FrenchMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "le", "la", "les", "de", "des", "du", "et", "a", "à", "au", "aux", "en", "un", "une",
        "ou", "d", "l", "y", "sur", "par", "pour", "avec", "sans", "dans", "qui", "que", "qu"
    };

    private static readonly Dictionary<string, string> OfficialFrenchTitleFallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Joseph Meeting His Brethren"] = "Joseph retrouvant ses frères",
        ["I Stand At The Door And Knock"] = "Je me tiens à la porte et je frappe",
        ["A Door In A Door"] = "Une porte dans une porte",
        ["The Breach Between The Seven Church Ages And The Seven Seals"] =
            "La brèche entre les sept âges de l'Église et les sept Sceaux",
        ["I Am The Resurrection And Life"] = "Je suis la résurrection et la vie",
        ["Doors In Door"] = "Des portes dans une porte",
        ["It Is The Rising Of The Sun"] = "C'est le lever du soleil"
    };

    [GeneratedRegex(@"\bA(été|coûté|commissionné|préparée|préparé|pourvu)\b", RegexOptions.IgnoreCase)]
    private static partial Regex GluedFrenchARegex();

    private static readonly HashSet<string> EnglishMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "The", "And", "Of", "For", "To", "In", "On", "With", "From", "That", "This", "Is", "Are"
    };

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();

    [GeneratedRegex(@"[^\p{L}\p{Nd}]+")]
    private static partial Regex NonLetterDigitRegex();

    [GeneratedRegex(@"^FRN\d", RegexOptions.IgnoreCase)]
    private static partial Regex SermonCodePrefixRegex();

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex PageNumberRegex();

    [GeneratedRegex(@"\s*-\s*")]
    private static partial Regex HyphenSpacingRegex();

    [GeneratedRegex(@"\s*,\s*")]
    private static partial Regex CommaSpacingRegex();

    [GeneratedRegex(@"(?<=\p{L})\(")]
    private static partial Regex MissingSpaceBeforeParenRegex();

    [GeneratedRegex(@"\s*['’]\s*")]
    private static partial Regex LooseApostropheRegex();

    [GeneratedRegex(@"\b([DdLlJjNnSsCc]|[Qq]u)'")]
    private static partial Regex ElidedPrefixRegex();

    private readonly record struct CoverWord(string Text, double Height, double Top, double Left, int LetterCount);
}
