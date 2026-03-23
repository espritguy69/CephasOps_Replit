using System.Text.RegularExpressions;

namespace CephasOps.Application.Common.Utilities;

/// <summary>
/// Utility for normalizing phone numbers for duplicate detection
/// </summary>
public static class PhoneNumberNormalizer
{
    /// <summary>
    /// Normalize phone number for comparison (remove spaces, dashes, parentheses, country codes)
    /// </summary>
    public static string Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        // Remove all non-digit characters
        var digits = Regex.Replace(phone, @"\D", "");

        // Remove Malaysian country code prefixes
        if (digits.StartsWith("60"))
        {
            digits = digits.Substring(2);
        }
        else if (digits.StartsWith("0060"))
        {
            digits = digits.Substring(4);
        }

        // Remove leading zero if present (domestic format)
        if (digits.StartsWith("0") && digits.Length > 1)
        {
            digits = digits.Substring(1);
        }

        return digits;
    }

    /// <summary>
    /// Check if two phone numbers are the same (after normalization)
    /// </summary>
    public static bool AreSame(string? phone1, string? phone2)
    {
        if (string.IsNullOrWhiteSpace(phone1) || string.IsNullOrWhiteSpace(phone2))
            return false;

        return Normalize(phone1) == Normalize(phone2);
    }
}

