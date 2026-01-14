using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AudioAssistant.Api.Services.Providers;

/// <summary>
/// Local Whisper.cpp HTTP endpoint provider
/// Fallback provider for offline/local transcription
/// </summary>
public class WhisperCppProvider : ITranscriptionProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhisperCppProvider> _logger;
    private readonly string _endpoint;

    public string ProviderName => "WhisperCpp";
    public IEnumerable<string> SupportedLanguages => new[]
    {
        "en", "es", "fr", "de", "it", "pt", "nl", "ru", "ja", "ko", "zh", "ar",
        "hi", "tr", "pl", "sv", "fi", "da", "no", "uk", "cs", "el", "he", "th", "vi",
        "id", "ms", "bn", "ta", "te", "mr", "ur", "fa", "sw", "vi"
    };
    public int? MaxAudioSizeMB => 500; // Local processing can handle larger files
    public decimal? CostPerMinute => null; // Free/local
    public bool RequiresApiKey => false; // Local instance

    public WhisperCppProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WhisperCppProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _endpoint = _configuration["WhisperCppSettings:Endpoint"]
            ?? "http://localhost:8080";

        // Set reasonable timeout for local processing
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<bool> IsAvailableAsync(string? apiKey = null)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_endpoint}/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Whisper.cpp provider availability check failed at {Endpoint}", _endpoint);
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
            _logger.LogInformation("Starting transcription with Whisper.cpp, language: {Language}", language);

            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(audioData), "file", "audio.mp3");
            
            // Whisper.cpp server parameters
            var parameters = new
            {
                language = language,
                temperature = 0.0f,
                word_timestamps = true,
                model = _configuration["WhisperCppSettings:Model"] ?? "base"
            };

            content.Add(
                new StringContent(System.Text.Json.JsonSerializer.Serialize(parameters)),
                "parameters");

            var response = await _httpClient.PostAsync($"{_endpoint}/inference", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Whisper.cpp error: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);

                throw new HttpRequestException(
                    $"Whisper.cpp returned {response.StatusCode}: {errorContent}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<WhisperCppResponse>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (apiResponse == null || string.IsNullOrEmpty(apiResponse.Text))
            {
                throw new InvalidOperationException("Whisper.cpp returned empty transcription");
            }

            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Whisper.cpp transcription completed in {Duration}ms, length: {Length}",
                duration.TotalMilliseconds, apiResponse.Text.Length);

            return new TranscriptionResult
            {
                Id = Guid.NewGuid(),
                Text = apiResponse.Text,
                Language = apiResponse.Language ?? language,
                Confidence = apiResponse.Confidence ?? 0.85f,
                Duration = duration,
                Provider = ProviderName,
                Tokens = 0, // Local processing doesn't track tokens
                Timestamp = DateTime.UtcNow,
                RawResponse = jsonContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Whisper.cpp transcription failed");
            throw;
        }
    }

    public async IAsyncEnumerable<TranscriptionChunk> TranscribeStreamingAsync(
        Stream audioStream,
        string? apiKey = null,
        string language = "en",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Streaming transcription not supported by Whisper.cpp endpoint");

        // Fall back to non-streaming transcription
        using var memoryStream = new MemoryStream();
        await audioStream.CopyToAsync(memoryStream, cancellationToken);
        var audioData = memoryStream.ToArray();

        var result = await TranscribeAsync(audioData, apiKey, language, cancellationToken);

        yield return new TranscriptionChunk
        {
            Index = 0,
            Text = result.Text,
            IsFinal = true,
            Confidence = result.Confidence,
            Timestamp = DateTime.UtcNow
        };
    }

    private class WhisperCppResponse
    {
        public string Text { get; set; } = string.Empty;
        public string? Language { get; set; }
        public float? Confidence { get; set; }
        public string? Duration { get; set; }
        public List<WhisperSegment>? Segments { get; set; }
    }

    private class WhisperSegment
    {
        public int Id { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public string Text { get; set; } = string.Empty;
        public float? Confidence { get; set; }
    }
}
