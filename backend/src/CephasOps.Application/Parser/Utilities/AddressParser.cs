using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;

namespace CephasOps.Application.Parser.Utilities;

/// <summary>
/// Utility for parsing and normalizing Malaysian addresses
/// </summary>
public static class AddressParser
{
    // Malaysian state names
    private static readonly string[] MalaysianStates = 
    {
        "Johor", "Kedah", "Kelantan", "Melaka", "Negeri Sembilan",
        "Pahang", "Perak", "Perlis", "Pulau Pinang", "Penang",
        "Sabah", "Sarawak", "Selangor", "Terengganu",
        "Wilayah Persekutuan", "WP", "Kuala Lumpur", "Labuan", "Putrajaya"
    };

    // Malaysian postcode ranges by state (for validation)
    private static readonly Dictionary<string, (int min, int max)[]> PostcodeRanges = new()
    {
        { "Johor", new[] { (80000, 86999) } },
        { "Kedah", new[] { (5000, 9999), (2000, 2999) } },
        { "Kelantan", new[] { (15000, 18999) } },
        { "Melaka", new[] { (75000, 78999) } },
        { "Negeri Sembilan", new[] { (70000, 74999) } },
        { "Pahang", new[] { (25000, 28999), (39000, 39999) } },
        { "Perak", new[] { (30000, 36999) } },
        { "Perlis", new[] { (1000, 2999) } },
        { "Pulau Pinang", new[] { (10000, 14999) } },
        { "Penang", new[] { (10000, 14999) } },
        { "Sabah", new[] { (88000, 91999) } },
        { "Sarawak", new[] { (93000, 98999) } },
        { "Selangor", new[] { (40000, 49999), (62000, 62999) } },
        { "Terengganu", new[] { (20000, 24999) } },
        { "Wilayah Persekutuan", new[] { (50000, 59999) } },
        { "Kuala Lumpur", new[] { (50000, 59999) } },
        { "Labuan", new[] { (87000, 87999) } },
        { "Putrajaya", new[] { (62000, 62999) } }
    };

    // Street name normalization dictionary (common abbreviations)
    private static readonly Dictionary<string, string> StreetNameNormalizations = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Jalan", "Jln" },
        { "Jln", "Jalan" },
        { "Jalan.", "Jalan" },
        { "Jl", "Jalan" },
        { "Lorong", "Lrg" },
        { "Lrg", "Lorong" },
        { "Lorong.", "Lorong" },
        { "Persiaran", "Persiaran" },
        { "Persiaran.", "Persiaran" },
        { "Taman", "Tmn" },
        { "Tmn", "Taman" },
        { "Taman.", "Taman" },
        { "Lebuh", "Lebuh" },
        { "Lebuhraya", "Lebuhraya" },
        { "Lebuhraya.", "Lebuhraya" }
    };

    /// <summary>
    /// Parse address text to extract components
    /// </summary>
    public static AddressComponents ParseAddress(string? addressText)
    {
        if (string.IsNullOrWhiteSpace(addressText))
        {
            return new AddressComponents
            {
                FullAddress = string.Empty
            };
        }

        var result = new AddressComponents
        {
            FullAddress = addressText
        };

        // Extract postcode (5-digit Malaysian postcode)
        var postcodeMatch = Regex.Match(addressText, @"\b(\d{5})\b");
        if (postcodeMatch.Success)
        {
            var postcode = postcodeMatch.Groups[1].Value;
            result.Postcode = postcode;
            
            // Validate postcode if state is known
            if (!string.IsNullOrEmpty(result.State) && !IsValidPostcodeForState(postcode, result.State))
            {
                // Postcode doesn't match state - might be wrong state detection
                // Keep postcode but log warning in future
            }
        }

        // Extract state
        foreach (var state in MalaysianStates)
        {
            if (addressText.Contains(state, StringComparison.OrdinalIgnoreCase))
            {
                result.State = state;
                break;
            }
        }

        // Handle WP/Wilayah Persekutuan special cases
        if (string.IsNullOrEmpty(result.State))
        {
            if (addressText.Contains("Kuala Lumpur", StringComparison.OrdinalIgnoreCase))
            {
                result.State = "Wilayah Persekutuan";
                result.City = "Kuala Lumpur";
            }
            else if (addressText.Contains("Putrajaya", StringComparison.OrdinalIgnoreCase))
            {
                result.State = "Wilayah Persekutuan";
                result.City = "Putrajaya";
            }
        }

        // Extract city - look for common patterns
        // Pattern: "XXXXX CityName, State" or "XXXXX CityName State"
        if (!string.IsNullOrEmpty(result.Postcode) && string.IsNullOrEmpty(result.City))
        {
            var cityPattern = $@"{result.Postcode}\s+([A-Za-z\s]+?)(?:,|\s+{result.State ?? ""}|$)";
            var cityMatch = Regex.Match(addressText, cityPattern, RegexOptions.IgnoreCase);
            if (cityMatch.Success)
            {
                result.City = cityMatch.Groups[1].Value.Trim().TrimEnd(',');
            }
        }

        // Extract unit number - common patterns
        var unitPatterns = new[]
        {
            @"Unit\s*(\d+[A-Za-z]?)",
            @"No\.?\s*(\d+[A-Za-z]?)",
            @"Level\s*(\d+[A-Za-z]?),?\s*Unit\s*(\d+[A-Za-z]?)"
        };

        foreach (var pattern in unitPatterns)
        {
            var unitMatch = Regex.Match(addressText, pattern, RegexOptions.IgnoreCase);
            if (unitMatch.Success)
            {
                result.UnitNo = unitMatch.Groups[unitMatch.Groups.Count > 2 ? 2 : 1].Value;
                break;
            }
        }

        // Extract building name - look for common building name patterns
        var buildingPatterns = new[]
        {
            @"([A-Z][A-Z\s]+(?:TOWER|RESIDENCE|APARTMENT|CONDO|CONDOMINIUM|COURT|HEIGHTS|PARK|POINT|SQUARE|PLAZA|MALL))",
            @"((?:MENARA|WISMA|KOMPLEKS|BANGUNAN)\s+[A-Z\s]+)"
        };

        foreach (var pattern in buildingPatterns)
        {
            var buildingMatch = Regex.Match(addressText, pattern, RegexOptions.IgnoreCase);
            if (buildingMatch.Success)
            {
                result.BuildingName = buildingMatch.Groups[1].Value.Trim();
                break;
            }
        }

        // Normalize street names in AddressLine1
        result.AddressLine1 = NormalizeStreetNames(addressText);
        
        // If AddressLine1 is same as full address, try to extract street name separately
        if (result.AddressLine1 == addressText)
        {
            var normalizedAddress = NormalizeStreetNames(addressText);
            if (normalizedAddress != addressText)
            {
                result.AddressLine1 = normalizedAddress;
            }
        }

        return result;
    }

    /// <summary>
    /// Validate if a postcode is valid for a given Malaysian state
    /// </summary>
    public static bool IsValidPostcodeForState(string postcode, string state)
    {
        if (string.IsNullOrWhiteSpace(postcode) || string.IsNullOrWhiteSpace(state))
            return false;

        if (!int.TryParse(postcode, out var postcodeInt))
            return false;

        // Normalize state name
        var normalizedState = NormalizeStateName(state);
        if (string.IsNullOrEmpty(normalizedState))
            return false;

        if (!PostcodeRanges.TryGetValue(normalizedState, out var ranges))
            return false;

        return ranges.Any(range => postcodeInt >= range.min && postcodeInt <= range.max);
    }

    /// <summary>
    /// Normalize state name (handle variations like "Pulau Pinang" vs "Penang")
    /// </summary>
    private static string? NormalizeStateName(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return null;

        var normalized = state.Trim();
        
        // Handle common variations
        if (normalized.Equals("Penang", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Pulau Pinang", StringComparison.OrdinalIgnoreCase))
            return "Pulau Pinang";

        if (normalized.Equals("WP", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Wilayah Persekutuan", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Kuala Lumpur", StringComparison.OrdinalIgnoreCase))
            return "Wilayah Persekutuan";

        return normalized;
    }

    /// <summary>
    /// Normalize street names (e.g., "Jln" -> "Jalan", "Lrg" -> "Lorong")
    /// </summary>
    public static string NormalizeStreetNames(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return address;

        var normalized = address;
        
        // Normalize common street name abbreviations
        foreach (var (abbrev, full) in StreetNameNormalizations)
        {
            // Replace abbreviations with full form (prefer full form for consistency)
            if (abbrev.Length < full.Length)
            {
                var pattern = $@"\b{Regex.Escape(abbrev)}\b";
                normalized = Regex.Replace(normalized, pattern, full, RegexOptions.IgnoreCase);
            }
        }

        return normalized;
    }

    /// <summary>
    /// Calculate Levenshtein distance between two strings (for fuzzy matching)
    /// </summary>
    public static int LevenshteinDistance(string s, string t)
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

    /// <summary>
    /// Fuzzy match building names (returns similarity score 0-1, where 1 is exact match).
    /// Both names are normalized for address abbreviations (Jln/Jalan, Tmn/Taman, Lrg/Lorong) before comparison.
    /// </summary>
    public static double FuzzyMatchBuildingName(string name1, string name2)
    {
        if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
            return 0.0;

        var normalized1 = NormalizeBuildingName(NormalizeStreetNames(name1));
        var normalized2 = NormalizeBuildingName(NormalizeStreetNames(name2));

        if (normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        var distance = LevenshteinDistance(normalized1, normalized2);
        var maxLength = Math.Max(normalized1.Length, normalized2.Length);
        
        if (maxLength == 0)
            return 1.0;

        return 1.0 - ((double)distance / maxLength);
    }

    /// <summary>
    /// Normalize building name for comparison (remove common words, normalize case)
    /// </summary>
    private static string NormalizeBuildingName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var normalized = name.Trim().ToUpperInvariant();
        
        // Remove common words that don't help with matching
        var commonWords = new[] { "THE", "A", "AN", "OF", "AND", "OR", "BUT" };
        foreach (var word in commonWords)
        {
            normalized = Regex.Replace(normalized, $@"\b{word}\b", " ", RegexOptions.IgnoreCase);
        }

        // Remove extra spaces
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }
}

/// <summary>
/// Parsed address components
/// </summary>
public class AddressComponents
{
    public string FullAddress { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string? UnitNo { get; set; }
    public string? BuildingName { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Postcode { get; set; }
}

