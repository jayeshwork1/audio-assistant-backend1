namespace AudioAssistant.Api.Services.Abstractions;

/// <summary>
/// Interface for STT transcription providers
/// Provides abstraction for different STT services (Groq, Whisper.cpp, OpenAI, Claude)
/// </summary>
public interface ITranscriptionProvider
{
    /// <summary>
    /// Transcribes audio data to text.
    /// </summary>
    Task<TranscriptionResult> TranscribeAsync(
        byte[] audioData, 
        string language = "en",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streaming transcription for real-time output.
    /// </summary>
    IAsyncEnumerable<TranscriptionChunk> TranscribeStreamingAsync(
        Stream audioStream,
        string language = "en",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Provider name identifier.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Whether this provider is currently available.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Supported languages for this provider.
    /// </summary>
    IEnumerable<string> SupportedLanguages { get; }

    /// <summary>
    /// Maximum audio file size in MB (null = unlimited).
    /// </summary>
    int? MaxAudioSizeMB { get; }

    /// <summary>
    /// Cost per minute of audio (null = free).
    /// </summary>
    decimal? CostPerMinute { get; }
}