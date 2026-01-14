using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace AudioAssistant.Tests;

/// <summary>
/// Unit tests for GroqWhisperProvider
/// </summary>
public class GroqWhisperProviderTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<GroqWhisperProvider>> _mockLogger;

    public GroqWhisperProviderTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.groq.com/openai/v1")
        };

        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["GroqSettings:ApiKey"])
            .Returns("test-groq-api-key");
        _mockConfiguration.Setup(c => c["GroqSettings:Endpoint"])
            .Returns("https://api.groq.com/openai/v1");

        _mockLogger = new Mock<ILogger<GroqWhisperProvider>>();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenApiReturnsSuccess_ReturnsTrue()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri?.PathAndQuery == "/models"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });

        var provider = new GroqWhisperProvider(
            _httpClient,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var result = await provider.IsAvailableAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenApiReturnsError_ReturnsFalse()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized
            });

        var provider = new GroqWhisperProvider(
            _httpClient,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var result = await provider.IsAvailableAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TranscribeAsync_WithValidAudio_ReturnsTranscription()
    {
        // Arrange
        var expectedResponse = new
        {
            text = "This is a test transcription",
            language = "en",
            duration = "1.5"
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri?.PathAndQuery == "/audio/transcriptions"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(expectedResponse),
                    Encoding.UTF8,
                    "application/json")
            });

        var provider = new GroqWhisperProvider(
            _httpClient,
            _mockConfiguration.Object,
            _mockLogger.Object);

        var audioData = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = await provider.TranscribeAsync(audioData, null, "en");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("This is a test transcription", result.Text);
        Assert.Equal("GroqWhisper", result.Provider);
    }

    [Fact]
    public async Task TranscribeAsync_WhenApiReturnsError_ThrowsException()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Invalid audio file\"}")
            });

        var provider = new GroqWhisperProvider(
            _httpClient,
            _mockConfiguration.Object,
            _mockLogger.Object);

        var audioData = new byte[] { 0x01, 0x02, 0x03 };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => provider.TranscribeAsync(audioData, null, "en"));
    }

    [Fact]
    public void ProviderName_ReturnsCorrectName()
    {
        // Arrange
        var provider = new GroqWhisperProvider(
            _httpClient,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var name = provider.ProviderName;

        // Assert
        Assert.Equal("GroqWhisper", name);
    }

    [Fact]
    public void RequiresApiKey_ReturnsFalse()
    {
        // Arrange
        var provider = new GroqWhisperProvider(
            _httpClient,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var requiresKey = provider.RequiresApiKey;

        // Assert
        Assert.False(requiresKey);
    }

    [Fact]
    public void SupportedLanguages_ContainsExpectedLanguages()
    {
        // Arrange
        var provider = new GroqWhisperProvider(
            _httpClient,
            _mockConfiguration.Object,
            _mockLogger.Object);

        // Act
        var languages = provider.SupportedLanguages;

        // Assert
        Assert.Contains("en", languages);
        Assert.Contains("es", languages);
        Assert.Contains("fr", languages);
    }

    [Fact]
    public async Task TranscribeStreamingAsync_ReturnsTranscription()
    {
        // Arrange
        var expectedResponse = new
        {
            text = "This is a test transcription",
            language = "en",
            duration = "1.5"
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri?.PathAndQuery == "/audio/transcriptions"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(expectedResponse),
                    Encoding.UTF8,
                    "application/json")
            });

        var provider = new GroqWhisperProvider(
            _httpClient,
            _mockConfiguration.Object,
            _mockLogger.Object);

        using var audioStream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });

        // Act
        var chunks = new List<TranscriptionChunk>();
        await foreach (var chunk in provider.TranscribeStreamingAsync(audioStream, null, "en"))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.Single(chunks);
        Assert.Equal("This is a test transcription", chunks[0].Text);
        Assert.True(chunks[0].IsFinal);
    }
}
