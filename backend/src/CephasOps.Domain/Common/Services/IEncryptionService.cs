namespace CephasOps.Domain.Common.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypt a plain text string
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypt an encrypted string
    /// </summary>
    string Decrypt(string encryptedText);
}

