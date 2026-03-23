using System.Text.RegularExpressions;

namespace CephasOps.Application.Parser.Utilities;

/// <summary>
/// Strict label normalization for Excel header/label matching.
/// Trim, collapse whitespace, remove punctuation (: . # -), normalize underscores to spaces, uppercase.
/// Supports exact match and safe "contains" match (e.g. "Customer Name (as per IC)").
/// </summary>
public static class ExcelLabelNormalizer
{
    private static readonly Regex WhitespaceCollapse = new Regex(@"\s+", RegexOptions.Compiled);
    private static readonly Regex PunctuationStrip = new Regex(@"[\:\.\#\-]+", RegexOptions.Compiled);

    /// <summary>
    /// Normalize a label for comparison: trim, collapse whitespace, remove : . # -, underscores to spaces, uppercase.
    /// </summary>
    public static string Normalize(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return string.Empty;

        var t = label.Trim();
        t = PunctuationStrip.Replace(t, " ");
        t = t.Replace("_", " ");
        t = WhitespaceCollapse.Replace(t, " ");
        t = t.Trim();
        return t.ToUpperInvariant();
    }

    /// <summary>
    /// True if normalized(cellValue) equals normalized(label).
    /// </summary>
    public static bool ExactMatch(string? cellValue, string? label)
    {
        return string.Equals(Normalize(cellValue), Normalize(label), StringComparison.Ordinal);
    }

    /// <summary>
    /// True if normalized(cellValue) contains normalized(label) or vice versa (safe contains).
    /// E.g. "Customer Name (as per IC)" matches label "Customer Name".
    /// </summary>
    public static bool ContainsMatch(string? cellValue, string? label)
    {
        var nCell = Normalize(cellValue);
        var nLabel = Normalize(label);
        if (string.IsNullOrEmpty(nLabel))
            return false;
        return nCell.Contains(nLabel, StringComparison.Ordinal) || nLabel.Contains(nCell, StringComparison.Ordinal);
    }

    /// <summary>
    /// True if cell value matches any of the label synonyms (exact or contains).
    /// </summary>
    public static bool MatchesAny(string? cellValue, IEnumerable<string> labelSynonyms)
    {
        if (string.IsNullOrWhiteSpace(cellValue) || labelSynonyms == null)
            return false;

        var nCell = Normalize(cellValue);
        foreach (var label in labelSynonyms)
        {
            if (string.IsNullOrWhiteSpace(label)) continue;
            var nLabel = Normalize(label);
            if (nCell == nLabel)
                return true;
            if (nCell.Contains(nLabel, StringComparison.Ordinal))
                return true;
            if (nLabel.Contains(nCell, StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
