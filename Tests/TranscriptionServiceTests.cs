using AudioAssistant.Api.Data;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services.Abstractions;
using AudioAssistant.Api.Services.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AudioAssistant.Tests;

/// <summary>
/// Integration tests for the TranscriptionService
/// </summary>
public class TranscriptionServiceTests
{
    private readonly Mock<IApiKeyService> _mockApiKeyService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<TranscriptionService>> _mockLogger;
    private readonly AudioAssistantDbContext _dbContext;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public TranscriptionServiceTests()
    {
        _mockApiKeyService = new Mock<IApiKeyService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<TranscriptionService>>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AudioAssistantDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AudioAssistantDbContext(options);
    }

    [Fact]
    public async Task TranscribeAsync_WithGroqProvider_ReturnsTranscription()
    {
        // Arrange
        var mockProvider = new Mock<ITranscriptionProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("GroqWhisper");
        mockProvider.Setup(p => p.RequiresApiKey).Returns(false);
        mockProvider.Setup(p => p.IsAvailableAsync(null))
            .ReturnsAsync(true);
        mockProvider.Setup(p => p.TranscribeAsync(
            It.IsAny<byte[]>(), 
            It.IsAny<string?>(), 
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResult
            {
                Id = Guid.NewGuid(),
                Text = "Test transcription",
                Language = "en",
                Confidence = 0.95f,
                Duration = TimeSpan.FromSeconds(1),
                Provider = "GroqWhisper",
                Timestamp = DateTime.UtcNow
            });

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(GroqWhisperProvider)))
            .Returns(mockProvider.Object);

        _mockConfiguration.Setup(c => c.GetSection("TranscriptionSettings:DefaultProvider"))
            .Returns(new Mock<IConfigurationSection>().Object);
        _mockConfiguration.Setup(c => c["TranscriptionSettings:DefaultProvider"])
            .Returns("GroqWhisper");
        _mockConfiguration.Setup(c => c.GetSection("TranscriptionSettings:FallbackChain"))
            .Returns(new Mock<IConfigurationSection>().Object);

        var service = new TranscriptionService(
            _dbContext,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockServiceProvider.Object,
            _mockApiKeyService.Object);

        var audioData = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = await service.TranscribeAsync(audioData, 1, "en");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test transcription", result.Text);
        Assert.Equal("GroqWhisper", result.Provider);
    }

    [Fact]
    public async Task TranscribeAsync_WithPrimaryProviderFailure_FallsBackToSecondary()
    {
        // Arrange
        var mockPrimaryProvider = new Mock<ITranscriptionProvider>();
        mockPrimaryProvider.Setup(p => p.ProviderName).Returns("GroqWhisper");
        mockPrimaryProvider.Setup(p => p.RequiresApiKey).Returns(false);
        mockPrimaryProvider.Setup(p => p.IsAvailableAsync(null))
            .ReturnsAsync(true);
        mockPrimaryProvider.Setup(p => p.TranscribeAsync(
            It.IsAny<byte[]>(), 
            It.IsAny<string?>(), 
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Primary provider failed"));

        var mockSecondaryProvider = new Mock<ITranscriptionProvider>();
        mockSecondaryProvider.Setup(p => p.ProviderName).Returns("WhisperCpp");
        mockSecondaryProvider.Setup(p => p.RequiresApiKey).Returns(false);
        mockSecondaryProvider.Setup(p => p.IsAvailableAsync(null))
            .ReturnsAsync(true);
        mockSecondaryProvider.Setup(p => p.TranscribeAsync(
            It.IsAny<byte[]>(), 
            It.IsAny<string?>(), 
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResult
            {
                Id = Guid.NewGuid(),
                Text = "Fallback transcription",
                Language = "en",
                Confidence = 0.90f,
                Duration = TimeSpan.FromSeconds(2),
                Provider = "WhisperCpp",
                Timestamp = DateTime.UtcNow
            });

        var callCount = 0;
        _mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Returns(new object[] { mockPrimaryProvider.Object, mockSecondaryProvider.Object }[callCount++]);

        _mockConfiguration.Setup(c => c["TranscriptionSettings:DefaultProvider"])
            .Returns("GroqWhisper");
        _mockConfiguration.Setup(c => c.GetSection("TranscriptionSettings:FallbackChain"))
            .Returns(new Mock<IConfigurationSection>().Object);

        var service = new TranscriptionService(
            _dbContext,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockServiceProvider.Object,
            _mockApiKeyService.Object);

        var audioData = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = await service.TranscribeAsync(audioData, 1, "en");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Fallback transcription", result.Text);
        Assert.Equal("WhisperCpp", result.Provider);
    }

    [Fact]
    public async Task TranscribeAsync_WithProviderRequiringApiKey_RetrievesAndUsesKey()
    {
        // Arrange
        var mockProvider = new Mock<ITranscriptionProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("OpenAIWhisper");
        mockProvider.Setup(p => p.RequiresApiKey).Returns(true);
        mockProvider.Setup(p => p.IsAvailableAsync("test-api-key"))
            .ReturnsAsync(true);
        mockProvider.Setup(p => p.TranscribeAsync(
            It.IsAny<byte[]>(), 
            "test-api-key", 
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranscriptionResult
            {
                Id = Guid.NewGuid(),
                Text = "API key transcription",
                Language = "en",
                Confidence = 0.97f,
                Duration = TimeSpan.FromSeconds(1),
                Provider = "OpenAIWhisper",
                Timestamp = DateTime.UtcNow
            });

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(OpenAIWhisperProvider)))
            .Returns(mockProvider.Object);

        _mockApiKeyService.Setup(s => s.GetApiKeyAsync(1, "OpenAIWhisper"))
            .ReturnsAsync((true, "test-api-key", null));

        _mockConfiguration.Setup(c => c["TranscriptionSettings:DefaultProvider"])
            .Returns("OpenAIWhisper");
        _mockConfiguration.Setup(c => c.GetSection("TranscriptionSettings:FallbackChain"))
            .Returns(new Mock<IConfigurationSection>().Object);

        var service = new TranscriptionService(
            _dbContext,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockServiceProvider.Object,
            _mockApiKeyService.Object);

        var audioData = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = await service.TranscribeAsync(audioData, 1, "en");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("API key transcription", result.Text);
        _mockApiKeyService.Verify(s => s.GetApiKeyAsync(1, "OpenAIWhisper"), Times.Once);
    }

    [Fact]
    public async Task SetPreferredProviderAsync_SavesToDatabase()
    {
        // Arrange
        var mockProvider = new Mock<ITranscriptionProvider>();
        _mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Returns(mockProvider.Object);

        _mockConfiguration.Setup(c => c["TranscriptionSettings:DefaultProvider"])
            .Returns("GroqWhisper");
        _mockConfiguration.Setup(c => c.GetSection("TranscriptionSettings:FallbackChain"))
            .Returns(new Mock<IConfigurationSection>().Object);

        var service = new TranscriptionService(
            _dbContext,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockServiceProvider.Object,
            _mockApiKeyService.Object);

        // Act
        await service.SetPreferredProviderAsync(1, "OpenAIWhisper");

        // Assert
        var preferences = await _dbContext.UserPreferences.FirstOrDefaultAsync(p => p.UserId == 1);
        Assert.NotNull(preferences);
        Assert.Equal("OpenAIWhisper", preferences.PreferredSTTProvider);
    }

    [Fact]
    public async Task TranscribeAsync_AllProvidersFail_ThrowsException()
    {
        // Arrange
        var mockProvider1 = new Mock<ITranscriptionProvider>();
        mockProvider1.Setup(p => p.ProviderName).Returns("GroqWhisper");
        mockProvider1.Setup(p => p.RequiresApiKey).Returns(false);
        mockProvider1.Setup(p => p.IsAvailableAsync(null)).ReturnsAsync(true);
        mockProvider1.Setup(p => p.TranscribeAsync(
            It.IsAny<byte[]>(), 
            It.IsAny<string?>(), 
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider 1 failed"));

        var mockProvider2 = new Mock<ITranscriptionProvider>();
        mockProvider2.Setup(p => p.ProviderName).Returns("WhisperCpp");
        mockProvider2.Setup(p => p.RequiresApiKey).Returns(false);
        mockProvider2.Setup(p => p.IsAvailableAsync(null)).ReturnsAsync(true);
        mockProvider2.Setup(p => p.TranscribeAsync(
            It.IsAny<byte[]>(), 
            It.IsAny<string?>(), 
            It.IsAny<string>(), 
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider 2 failed"));

        var callCount = 0;
        _mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Returns(new object[] { mockProvider1.Object, mockProvider2.Object }[callCount++]);

        _mockConfiguration.Setup(c => c["TranscriptionSettings:DefaultProvider"])
            .Returns("GroqWhisper");
        _mockConfiguration.Setup(c => c.GetSection("TranscriptionSettings:FallbackChain"))
            .Returns(new Mock<IConfigurationSection>().Object);

        var service = new TranscriptionService(
            _dbContext,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockServiceProvider.Object,
            _mockApiKeyService.Object);

        var audioData = new byte[] { 0x01, 0x02, 0x03 };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TranscribeAsync(audioData, 1, "en"));
    }
}
