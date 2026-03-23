namespace CephasOps.Application.Parser.Utilities;

/// <summary>
/// Utility for normalizing Malaysian phone numbers
/// </summary>
public static class PhoneNumberUtility
{
    /// <summary>
    /// Auto-fix Malaysian phone numbers to standard format (0XXXXXXXXX)
    /// </summary>
    /// <remarks>
    /// Rules:
    /// - Remove symbols: +, -, spaces
    /// - Convert +60XXXXXXXX → 0XXXXXXXX
    /// - If the number is 9 digits → prefix 0
    /// - If the number starts with 1 and has 8–10 digits → prefix 0
    /// </remarks>
    public static string NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return string.Empty;
        }

        // Remove all non-digit characters
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(digits))
        {
            return string.Empty;
        }

        // Handle +60 prefix (Malaysian country code)
        if (digits.StartsWith("60") && digits.Length >= 11)
        {
            digits = "0" + digits[2..];
        }

        // If 9 digits, prefix with 0
        if (digits.Length == 9)
        {
            digits = "0" + digits;
        }

        // If starts with 1 and has 8-10 digits, prefix with 0
        if (digits.StartsWith("1") && digits.Length >= 8 && digits.Length <= 10)
        {
            digits = "0" + digits;
        }

        return digits;
    }

    /// <summary>
    /// Validate if phone number is in valid Malaysian format after normalization
    /// </summary>
    public static bool IsValidMalaysianPhone(string? phoneNumber)
    {
        var normalized = NormalizePhoneNumber(phoneNumber);
        
        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        // Malaysian mobile numbers: 01X-XXXXXXX (10-11 digits starting with 01)
        // Malaysian landlines: 0X-XXXXXXX (9-10 digits starting with 0)
        return normalized.StartsWith("0") && normalized.Length >= 9 && normalized.Length <= 11;
    }

    /// <summary>
    /// Normalize Malaysian MSISDN (alias for NormalizePhoneNumber for consistency with docs)
    /// </summary>
    /// <remarks>
    /// Goal: Fix missing leading zeros and strip formatting.
    /// Examples:
    /// - "12-216 4657" → "0122164657"
    /// - "(017) 889-9331" → "0178899331"
    /// - Length 9: prepend "0" → 10 digits
    /// - Length 10: prepend "0" → 11 digits
    /// - Length 11: keep as is
    /// </remarks>
    public static string? NormalizeMalaysianMsisdn(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Remove all non-digit characters
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(digits))
            return null;

        // Handle +60 prefix (Malaysian country code)
        if (digits.StartsWith("60") && digits.Length >= 11)
        {
            digits = "0" + digits[2..];
        }

        // Length-based rules per the parser spec
        return digits.Length switch
        {
            9 => "0" + digits,   // 9 digits → prepend 0 → 10 digits
            10 when !digits.StartsWith("0") => "0" + digits, // 10 digits not starting with 0 → prepend 0
            _ => digits.StartsWith("0") || digits.Length >= 11 ? digits : "0" + digits
        };
    }
}

