using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AudioAssistant.Api.Services.Providers;

/// <summary>
/// Groq Whisper API transcription provider
/// Primary provider using Groq's fast Whisper implementation
/// </summary>
public class GroqWhisperProvider : ITranscriptionProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroqWhisperProvider> _logger;
    private readonly string _apiKey;

    public string ProviderName => "GroqWhisper";
    public IEnumerable<string> SupportedLanguages => new[]
    {
        "en", "es", "fr", "de", "it", "pt", "nl", "ru", "ja", "ko", "zh", "ar",
        "hi", "tr", "pl", "sv", "fi", "da", "no", "uk", "cs", "el", "he", "th", "vi"
    };
    public int? MaxAudioSizeMB => 25;
    public decimal? CostPerMinute => null; // Check current pricing
    public bool RequiresApiKey => false; // Server-side configured

    public GroqWhisperProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GroqWhisperProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _apiKey = _configuration["GroqSettings:ApiKey"]
            ?? throw new InvalidOperationException("Groq API key not configured");

        var endpoint = _configuration["GroqSettings:Endpoint"]
            ?? "https://api.groq.com/openai/v1";

        _httpClient.BaseAddress = new Uri(endpoint);
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<bool> IsAvailableAsync(string? apiKey = null)
    {
        try
        {
            var response = await _httpClient.GetAsync("/models");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Groq provider availability check failed");
            return false;
        }
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        byte[] audioData,
        string? apiKey = null,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting transcription with Groq Whisper, language: {Language}", language);

            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(audioData), "file", "audio.mp3");
            content.Add(new StringContent("whisper-large-v3"), "model");
            content.Add(new StringContent("json"), "response_format");
            content.Add(new StringContent(language), "language");
            content.Add(new StringContent("false"), "temperature");

            var response = await _httpClient.PostAsync("/audio/transcriptions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Groq API error: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);

                throw new HttpRequestException(
                    $"Groq API returned {response.StatusCode}: {errorContent}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<GroqTranscriptionResponse>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (apiResponse == null || string.IsNullOrEmpty(apiResponse.Text))
            {
                throw new InvalidOperationException("Groq returned empty transcription");
            }

            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Groq transcription completed in {Duration}ms, length: {Length}",
                duration.TotalMilliseconds, apiResponse.Text.Length);

            return new TranscriptionResult
            {
                Id = Guid.NewGuid(),
                Text = apiResponse.Text,
                Language = apiResponse.Language ?? language,
                Confidence = 0.95f, // Whisper doesn't provide confidence, use default
                Duration = duration,
                Provider = ProviderName,
                Tokens = apiResponse.Tokens ?? 0,
                Timestamp = DateTime.UtcNow,
                RawResponse = jsonContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq transcription failed");
            throw;
        }
    }

    public async IAsyncEnumerable<TranscriptionChunk> TranscribeStreamingAsync(
        Stream audioStream,
        string? apiKey = null,
        string language = "en",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Streaming transcription not supported by Groq Whisper API");
        
        // Fall back to non-streaming transcription
        using var memoryStream = new MemoryStream();
        await audioStream.CopyToAsync(memoryStream, cancellationToken);
        var audioData = memoryStream.ToArray();

        var result = await TranscribeAsync(audioData, language, cancellationToken);

        yield return new TranscriptionChunk
        {
            Index = 0,
            Text = result.Text,
            IsFinal = true,
            Confidence = result.Confidence,
            Timestamp = DateTime.UtcNow
        };
    }

    private class GroqTranscriptionResponse
    {
        public string Text { get; set; } = string.Empty;
        public string? Language { get; set; }
        public int? Tokens { get; set; }
        public string? Duration { get; set; }
        public List<WordSegment>? Words { get; set; }
        public List<Segment>? Segments { get; set; }
    }

    private class WordSegment
    {
        public string Word { get; set; } = string.Empty;
        public double Start { get; set; }
        public double End { get; set; }
    }

    private class Segment
    {
        public int Id { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
