using System.Security.Cryptography;
using System.Text;
using CephasOps.Domain.Common.Services;
using Microsoft.Extensions.Configuration;

namespace CephasOps.Infrastructure.Security;

/// <summary>
/// AES encryption service for sensitive data
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IConfiguration configuration)
    {
        // Get encryption key from configuration or use default (should be in appsettings)
        var encryptionKey = configuration["Encryption:Key"] ?? 
            "DefaultEncryptionKey32Chars!!"; // 32 characters for AES-256
        
        if (encryptionKey.Length < 32)
        {
            // Pad or hash to 32 bytes
            using var sha256 = SHA256.Create();
            _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
        }
        else
        {
            _key = Encoding.UTF8.GetBytes(encryptionKey.Substring(0, 32));
        }

        // IV should be 16 bytes for AES
        var ivString = configuration["Encryption:IV"] ?? "DefaultIV16Bytes!";
        if (ivString.Length < 16)
        {
            using var sha256 = SHA256.Create();
            var ivHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(ivString));
            _iv = new byte[16];
            Array.Copy(ivHash, _iv, 16);
        }
        else
        {
            _iv = Encoding.UTF8.GetBytes(ivString.Substring(0, 16));
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Convert.ToBase64String(encryptedBytes);
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // If decryption fails, return empty string
            return string.Empty;
        }
    }
}

