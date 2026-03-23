using System.Security.Cryptography;
using System.Text;
using CephasOps.Application.Common.Interfaces;

namespace CephasOps.Application.Common.Services;

/// <summary>
/// Password hasher that verifies legacy (SHA256 + fixed salt) hashes and writes modern (BCrypt) hashes.
/// Legacy format: Base64(SHA256(password + salt)). Modern format: BCrypt (starts with $2).
/// </summary>
public sealed class CompatibilityPasswordHasher : IPasswordHasher
{
    private const string LegacySalt = "CephasOps_Salt_2024";

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10);
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return false;

        if (IsLegacyHash(storedHash))
            return VerifyLegacy(password, storedHash);

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
        catch
        {
            return false;
        }
    }

    public bool NeedsRehash(string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return true;
        return IsLegacyHash(storedHash);
    }

    private static bool IsLegacyHash(string hash)
    {
        return !hash.StartsWith("$2", StringComparison.Ordinal);
    }

    private static bool VerifyLegacy(string password, string hash)
    {
        var computed = ComputeLegacyHash(password);
        return computed == hash;
    }

    private static string ComputeLegacyHash(string password)
    {
        using var sha256 = SHA256.Create();
        var salted = password + LegacySalt;
        var bytes = Encoding.UTF8.GetBytes(salted);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
