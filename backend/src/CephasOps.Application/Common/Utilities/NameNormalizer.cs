using System.Text;
using System.Text.RegularExpressions;

namespace CephasOps.Application.Common.Utilities;

/// <summary>
/// Utility for normalizing names for duplicate detection
/// </summary>
public static class NameNormalizer
{
    /// <summary>
    /// Normalize a name for comparison (remove extra spaces, convert to uppercase, remove special characters)
    /// </summary>
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Remove extra whitespace
        var normalized = Regex.Replace(name.Trim(), @"\s+", " ");

        // Convert to uppercase for case-insensitive comparison
        normalized = normalized.ToUpperInvariant();

        // Remove common prefixes/suffixes that don't affect identity
        normalized = normalized.Replace("BIN ", "BIN")
                               .Replace(" BIN ", " ")
                               .Replace("BINTI ", "BINTI")
                               .Replace(" BINTI ", " ");

        return normalized;
    }

    /// <summary>
    /// Calculate similarity score between two names (0-1, where 1 is exact match)
    /// Uses Levenshtein distance
    /// </summary>
    public static double CalculateSimilarity(string name1, string name2)
    {
        var normalized1 = Normalize(name1);
        var normalized2 = Normalize(name2);

        if (normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        if (string.IsNullOrEmpty(normalized1) || string.IsNullOrEmpty(normalized2))
            return 0.0;

        var distance = LevenshteinDistance(normalized1, normalized2);
        var maxLength = Math.Max(normalized1.Length, normalized2.Length);

        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Check if two names are likely the same person (similarity threshold: 0.85)
    /// </summary>
    public static bool IsLikelySamePerson(string name1, string name2, double threshold = 0.85)
    {
        return CalculateSimilarity(name1, name2) >= threshold;
    }

    private static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
            return string.IsNullOrEmpty(t) ? 0 : t.Length;

        if (string.IsNullOrEmpty(t))
            return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        // Initialize
        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        // Fill matrix
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}

