using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AudioAssistant.Api.Models;

/// <summary>
/// Represents a meeting
/// </summary>
public class Meeting
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ConversationId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? Duration { get; set; }

    public string? Participants { get; set; }

    [MaxLength(50)]
    public string? Type { get; set; }

    [MaxLength(100)]
    public string? Domain { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ConversationId))]
    public Conversation Conversation { get; set; } = null!;

    public MeetingNotes? Notes { get; set; }
}
