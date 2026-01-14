using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AudioAssistant.Api.Models;
using AudioAssistant.Api.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AudioAssistant.Api.Services.Providers;

/// <summary>
/// Claude Haiku transcription provider (placeholder for future support)
/// Note: Claude models currently do not support audio transcription
/// This provider is provided for future compatibility when/if Anthropic adds audio support
/// </summary>
public class ClaudeHaikuSTTProvider : ITranscriptionProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClaudeHaikuSTTProvider> _logger;

    public string ProviderName => "ClaudeHaiku";
    public IEnumerable<string> SupportedLanguages => Array.Empty<string>();
    public int? MaxAudioSizeMB => null;
    public decimal? CostPerMinute => null;
    public bool RequiresApiKey => true;

    public ClaudeHaikuSTTProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ClaudeHaikuSTTProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var endpoint = _configuration["ClaudeSettings:Endpoint"]
            ?? "https://api.anthropic.com/v1";

        _httpClient.BaseAddress = new Uri(endpoint);
    }

    public Task<bool> IsAvailableAsync(string? apiKey = null)
    {
        // Claude Haiku does not currently support audio transcription
        _logger.LogInformation("Claude Haiku provider: Audio transcription not currently supported");
        return Task.FromResult(false);
    }

    public Task<TranscriptionResult> TranscribeAsync(
        byte[] audioData,
        string? apiKey = null,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Claude Haiku transcription attempted but audio transcription is not supported");
        throw new NotSupportedException(
            "Claude Haiku does not currently support audio transcription. " +
            "Use Groq, OpenAI, or Whisper.cpp providers instead.");
    }

    public async IAsyncEnumerable<TranscriptionChunk> TranscribeStreamingAsync(
        Stream audioStream,
        string? apiKey = null,
        string language = "en",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Claude Haiku streaming transcription attempted but not supported");
        throw new NotSupportedException(
            "Claude Haiku does not currently support audio transcription. " +
            "Use Groq, OpenAI, or Whisper.cpp providers instead.");
    }
}
