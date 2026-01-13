using System.Security.Cryptography;
using System.Text;

namespace AudioAssistant.Api.Utilities;

/// <summary>
/// Service for encrypting and decrypting sensitive data using AES-256
/// </summary>
public class EncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(string encryptionKey)
    {
        // Ensure the key is 32 bytes (256 bits) for AES-256
        using var sha256 = SHA256.Create();
        _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
    }

    /// <summary>
    /// Encrypts a string using AES-256
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <returns>Base64-encoded encrypted string</returns>
    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var msEncrypt = new MemoryStream();
        
        // Write IV to the beginning of the stream
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);
        
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    /// <summary>
    /// Decrypts an AES-256 encrypted string
    /// </summary>
    /// <param name="cipherText">Base64-encoded encrypted string</param>
    /// <returns>Decrypted plain text</returns>
    public string Decrypt(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        // Extract IV from the beginning of the cipher text
        var iv = new byte[aes.IV.Length];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        return srDecrypt.ReadToEnd();
    }
}
