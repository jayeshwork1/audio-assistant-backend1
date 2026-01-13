using System.ComponentModel.DataAnnotations;

namespace AudioAssistant.Api.Models;

/// <summary>
/// Represents the result of a transcription operation
/// </summary>
public class TranscriptionResult
{
    /// <summary>
    /// Unique identifier for this transcription result
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The transcribed text
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Detected or specified language code (e.g., "en", "es")
    /// </summary>
    [MaxLength(10)]
    public string Language { get; set; } = "en";

    /// <summary>
    /// Confidence score (0-1) indicating transcription accuracy
    /// </summary>
    [Range(0.0, 1.0)]
    public float Confidence { get; set; } = 0.0f;

    /// <summary>
    /// Duration of the audio that was transcribed
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Name of the provider that performed the transcription
    /// </summary>
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Number of tokens used (if applicable for API-based providers)
    /// </summary>
    public int Tokens { get; set; } = 0;

    /// <summary>
    /// Timestamp when transcription was completed
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Optional raw API response for debugging purposes
    /// </summary>
    public string? RawResponse { get; set; }
}