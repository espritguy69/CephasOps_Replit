namespace CephasOps.Application.Parser.Utilities;

/// <summary>
/// Deterministic string normalization for parser material names and alias lookup.
/// Used consistently by: direct ItemCode/Description match, alias storage, alias lookup.
/// </summary>
public static class MaterialNameNormalizer
{
    /// <summary>
    /// Normalize a material name for comparison and storage.
    /// Trim, collapse whitespace, no case change (comparisons use OrdinalIgnoreCase).
    /// </summary>
    public static string? Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var t = name.Trim();
        if (t.Length == 0) return null;
        return string.Join(" ", t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
