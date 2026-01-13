using Microsoft.EntityFrameworkCore;
using AudioAssistant.Api.Models;

namespace AudioAssistant.Api.Data;

/// <summary>
/// Database context for the Audio Assistant application
/// </summary>
public class AudioAssistantDbContext : DbContext
{
    public AudioAssistantDbContext(DbContextOptions<AudioAssistantDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ConversationExchange> ConversationExchanges { get; set; }
    public DbSet<Transcript> Transcripts { get; set; }
    public DbSet<Translation> Translations { get; set; }
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<MeetingNotes> MeetingNotes { get; set; }
    public DbSet<Export> Exports { get; set; }
    public DbSet<TransactionLog> TransactionLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configurations
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasOne(e => e.Preferences)
                .WithOne(e => e.User)
                .HasForeignKey<UserPreferences>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiKey configurations
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Provider }).IsUnique();
        });

        // Conversation configurations
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.UserId);
        });

        // ConversationExchange configurations
        modelBuilder.Entity<ConversationExchange>(entity =>
        {
            entity.HasIndex(e => new { e.ConversationId, e.Sequence });
        });

        // Meeting configurations
        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasOne(e => e.Notes)
                .WithOne(e => e.Meeting)
                .HasForeignKey<MeetingNotes>(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TransactionLog configurations
        modelBuilder.Entity<TransactionLog>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
