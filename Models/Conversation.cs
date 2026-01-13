using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AudioAssistant.Api.Models;

/// <summary>
/// Represents a conversation session
/// </summary>
public class Conversation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SessionId { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Title { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public int? Duration { get; set; }

    [MaxLength(50)]
    public string? MeetingType { get; set; }

    [MaxLength(100)]
    public string? Domain { get; set; }

    [MaxLength(10)]
    public string? Language { get; set; }

    public int? ParticipantCount { get; set; }

    public string? Summary { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<ConversationExchange> Exchanges { get; set; } = new List<ConversationExchange>();
    public ICollection<Transcript> Transcripts { get; set; } = new List<Transcript>();
    public Meeting? Meeting { get; set; }
}
