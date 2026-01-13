using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AudioAssistant.Api.Models;

/// <summary>
/// Stores translations of transcripts
/// </summary>
public class Translation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SourceTranscriptId { get; set; }

    [Required]
    [MaxLength(10)]
    public string TargetLanguage { get; set; } = string.Empty;

    [Required]
    public string TranslatedText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(SourceTranscriptId))]
    public Transcript SourceTranscript { get; set; } = null!;
}
