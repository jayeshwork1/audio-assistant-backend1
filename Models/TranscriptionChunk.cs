namespace AudioAssistant.Api.Models;

/// <summary>
/// Represents a chunk of transcription for streaming output
/// </summary>
public class TranscriptionChunk
{
    /// <summary>
    /// Chunk sequence number
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Partial or final transcription text for this chunk
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// True when this chunk contains the final transcription
    /// </summary>
    public bool IsFinal { get; set; } = false;

    /// <summary>
    /// Confidence score for this specific chunk
    /// </summary>
    [Range(0.0, 1.0)]
    public float Confidence { get; set; } = 0.0f;

    /// <summary>
    /// Timestamp when this chunk was received
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}