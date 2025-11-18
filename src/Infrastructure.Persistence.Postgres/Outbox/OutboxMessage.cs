using Microsoft.EntityFrameworkCore;

namespace Netemplate.Infrastructure.Persistence.Postgres.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}

public static class OutboxConfiguration
{
    public static void ConfigureOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.RetryCount).HasDefaultValue(0);

            entity.HasIndex(e => new { e.ProcessedAt, e.CreatedAt });
        });
    }
}
