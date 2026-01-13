using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AudioAssistant.Api.Models;

/// <summary>
/// Represents a single exchange within a conversation
/// </summary>
public class ConversationExchange
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ConversationId { get; set; }

    [Required]
    public int Sequence { get; set; }

    [Required]
    public string UserInput { get; set; } = string.Empty;

    public DateTime InputTimestamp { get; set; } = DateTime.UtcNow;

    public string? AiResponse { get; set; }

    public DateTime? ResponseTimestamp { get; set; }

    [MaxLength(50)]
    public string? ResponseStyle { get; set; }

    [MaxLength(50)]
    public string? AiProvider { get; set; }

    public string? ContextUsed { get; set; }

    public string? Metadata { get; set; }

    // Navigation property
    [ForeignKey(nameof(ConversationId))]
    public Conversation Conversation { get; set; } = null!;
}
