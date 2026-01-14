using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AudioAssistant.Api.Services.Providers;

/// <summary>
/// OpenAI Whisper API transcription provider
/// Uses user-provided API key for transcription
/// </summary>
public class OpenAIWhisperProvider : ITranscriptionProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIWhisperProvider> _logger;

    public string ProviderName => "OpenAIWhisper";
    public IEnumerable<string> SupportedLanguages => new[]
    {
        "en", "es", "fr", "de", "it", "pt", "nl", "ru", "ja", "ko", "zh", "ar",
        "hi", "tr", "pl", "sv", "fi", "da", "no", "uk", "cs", "el", "he", "th", "vi",
        "id", "ms", "bn", "ta", "te", "mr", "ur", "fa"
    };
    public int? MaxAudioSizeMB => 25;
    public decimal? CostPerMinute => 0.006m; // $0.006 per minute for whisper-1
    public bool RequiresApiKey => true; // User-provided API key required

    public OpenAIWhisperProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIWhisperProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var endpoint = _configuration["OpenAISettings:Endpoint"]
            ?? "https://api.openai.com/v1";

        _httpClient.BaseAddress = new Uri(endpoint);
    }

    public async Task<bool> IsAvailableAsync(string? apiKey = null)
    {
        try
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("OpenAI provider: No API key available");
                return false;
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.GetAsync("/models");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI provider availability check failed");
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
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key not provided");
            }

            _logger.LogInformation("Starting transcription with OpenAI Whisper, language: {Language}", language);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(audioData), "file", "audio.mp3");
            content.Add(new StringContent("whisper-1"), "model");
            content.Add(new StringContent(language), "language");
            content.Add(new StringContent("json"), "response_format");

            var response = await _httpClient.PostAsync("/audio/transcriptions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("OpenAI API error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                throw new HttpRequestException(
                    $"OpenAI API returned {response.StatusCode}: {errorContent}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<OpenAITranscriptionResponse>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (apiResponse == null || string.IsNullOrEmpty(apiResponse.Text))
            {
                throw new InvalidOperationException("OpenAI returned empty transcription");
            }

            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("OpenAI transcription completed in {Duration}ms, length: {Length}",
                duration.TotalMilliseconds, apiResponse.Text.Length);

            return new TranscriptionResult
            {
                Id = Guid.NewGuid(),
                Text = apiResponse.Text,
                Language = apiResponse.Language ?? language,
                Confidence = 0.97f, // Whisper generally has high accuracy
                Duration = duration,
                Provider = ProviderName,
                Tokens = apiResponse.Tokens ?? 0,
                Timestamp = DateTime.UtcNow,
                RawResponse = jsonContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI transcription failed");
            throw;
        }
    }

    public async IAsyncEnumerable<TranscriptionChunk> TranscribeStreamingAsync(
        Stream audioStream,
        string? apiKey = null,
        string language = "en",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Streaming transcription not supported by OpenAI Whisper API");

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

    private class OpenAITranscriptionResponse
    {
        public string Text { get; set; } = string.Empty;
        public string? Language { get; set; }
        public int? Tokens { get; set; }
        public string? Duration { get; set; }
        public List<OpenAIWordSegment>? Words { get; set; }
        public List<OpenAISegment>? Segments { get; set; }
    }

    private class OpenAIWordSegment
    {
        public string Word { get; set; } = string.Empty;
        public double Start { get; set; }
        public double End { get; set; }
    }

    private class OpenAISegment
    {
        public int Id { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
