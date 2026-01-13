using AudioAssistant.Api.Utilities;
using Xunit;

namespace AudioAssistant.Tests;

public class EncryptionServiceTests
{
    private readonly EncryptionService _encryptionService;
    private const string EncryptionKey = "test-encryption-key-for-testing";

    public EncryptionServiceTests()
    {
        _encryptionService = new EncryptionService(EncryptionKey);
    }

    [Fact]
    public void Encrypt_ShouldReturnEncryptedString()
    {
        // Arrange
        var plainText = "sensitive-api-key-12345";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(plainText, encrypted);
    }

    [Fact]
    public void Decrypt_ShouldReturnOriginalString()
    {
        // Arrange
        var plainText = "sensitive-api-key-12345";
        var encrypted = _encryptionService.Encrypt(plainText);

        // Act
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void Encrypt_ShouldGenerateDifferentCipherTexts()
    {
        // Arrange
        var plainText = "sensitive-api-key-12345";

        // Act
        var encrypted1 = _encryptionService.Encrypt(plainText);
        var encrypted2 = _encryptionService.Encrypt(plainText);

        // Assert - Different because of random IV
        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public void EncryptDecrypt_WithEmptyString_ShouldWork()
    {
        // Arrange
        var plainText = "";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_WithLongString_ShouldWork()
    {
        // Arrange
        var plainText = new string('A', 1000);

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_WithSpecialCharacters_ShouldWork()
    {
        // Arrange
        var plainText = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";

        // Act
        var encrypted = _encryptionService.Encrypt(plainText);
        var decrypted = _encryptionService.Decrypt(encrypted);

        // Assert
        Assert.Equal(plainText, decrypted);
    }
}
