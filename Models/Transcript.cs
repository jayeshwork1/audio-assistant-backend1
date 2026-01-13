using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AudioAssistant.Api.Models;

/// <summary>
/// Stores transcripts from audio conversations
/// </summary>
public class Transcript
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ConversationId { get; set; }

    [Required]
    public string RawTranscript { get; set; } = string.Empty;

    public string? ProcessedTranscript { get; set; }

    [MaxLength(10)]
    public string? Language { get; set; }

    [Range(0.0, 1.0)]
    public double? Confidence { get; set; }

    [MaxLength(100)]
    public string? Speaker { get; set; }

    public string? Timestamps { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ConversationId))]
    public Conversation Conversation { get; set; } = null!;

    public ICollection<Translation> Translations { get; set; } = new List<Translation>();
}
