namespace AudioAssistant.Api.Models.DTOs;

/// <summary>
/// Response DTO for audio transcription
/// </summary>
public class TranscriptionResponse
{
    /// <summary>
    /// Unique identifier for the transcription
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The transcribed text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Detected or specified language code
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score (0-1)
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Processing duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Name of the provider used
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Number of tokens used (if applicable)
    /// </summary>
    public int Tokens { get; set; }

    /// <summary>
    /// Timestamp of completion
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Whether a fallback was used
    /// </summary>
    public bool UsedFallback { get; set; }
}
