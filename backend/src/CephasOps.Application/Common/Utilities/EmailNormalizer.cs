namespace CephasOps.Application.Common.Utilities;

/// <summary>
/// Utility for normalizing email addresses for duplicate detection
/// </summary>
public static class EmailNormalizer
{
    /// <summary>
    /// Normalize email address for comparison (lowercase, trim)
    /// </summary>
    public static string Normalize(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Check if two email addresses are the same (after normalization)
    /// </summary>
    public static bool AreSame(string? email1, string? email2)
    {
        if (string.IsNullOrWhiteSpace(email1) || string.IsNullOrWhiteSpace(email2))
            return false;

        return Normalize(email1) == Normalize(email2);
    }
}

