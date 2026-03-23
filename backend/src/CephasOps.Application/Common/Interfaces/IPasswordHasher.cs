namespace CephasOps.Application.Common.Interfaces;

/// <summary>
/// Password hashing and verification with support for legacy and modern formats.
/// Existing (legacy) hashes continue to verify; new passwords are stored in modern format.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a password using the current modern algorithm. Use for all new or changed passwords.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against a stored hash. Supports both legacy and modern hash formats.
    /// </summary>
    bool VerifyPassword(string password, string storedHash);

    /// <summary>
    /// Returns true if the stored hash should be rehashed (e.g. legacy format). Caller may rehash on next successful login.
    /// </summary>
    bool NeedsRehash(string storedHash);
}
