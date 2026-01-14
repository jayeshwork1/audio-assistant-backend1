using System.ComponentModel.DataAnnotations;

namespace AudioAssistant.Api.Models.DTOs;

/// <summary>
/// Request DTO for audio transcription
/// </summary>
public class TranscriptionRequest
{
    /// <summary>
    /// Audio data in bytes (base64 encoded or raw bytes)
    /// </summary>
    [Required]
    [MaxLength(25 * 1024 * 1024)] // Max 25MB
    public byte[] AudioData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Language code (e.g., "en", "es", "fr")
    /// </summary>
    [MaxLength(10)]
    public string? Language { get; set; }

    /// <summary>
    /// Optional specific provider to use (overrides user preference)
    /// </summary>
    [MaxLength(50)]
    public string? Provider { get; set; }

    /// <summary>
    /// Whether to use streaming transcription (if supported)
    /// </summary>
    public bool Streaming { get; set; } = false;
}

/// <summary>
/// Request DTO for setting preferred provider
/// </summary>
public class SetProviderRequest
{
    public string Provider { get; set; } = string.Empty;
}
