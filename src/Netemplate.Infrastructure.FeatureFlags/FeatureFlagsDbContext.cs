using Microsoft.EntityFrameworkCore;
using Netemplate.Infrastructure.FeatureFlags.Entities;

namespace Netemplate.Infrastructure.FeatureFlags;

/// <summary>
/// DbContext for feature flags and their audit trail
/// </summary>
public sealed class FeatureFlagsDbContext : DbContext
{
    public FeatureFlagsDbContext(DbContextOptions<FeatureFlagsDbContext> options) : base(options)
    {
    }

    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<FeatureFlagAuditLog> FeatureFlagAuditLogs => Set<FeatureFlagAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FeatureFlag>(entity =>
        {
            entity.ToTable("feature_flags");
            entity.HasKey(f => f.Id);

            entity.Property(f => f.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(f => f.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(f => f.IsEnabled)
                .HasColumnName("is_enabled")
                .IsRequired();

            entity.Property(f => f.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            entity.Property(f => f.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(f => f.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.Property(f => f.CreatedBy)
                .HasColumnName("created_by")
                .HasMaxLength(200);

            entity.Property(f => f.UpdatedBy)
                .HasColumnName("updated_by")
                .HasMaxLength(200);

            entity.Property(f => f.Tags)
                .HasColumnName("tags")
                .HasMaxLength(500);

            entity.HasIndex(f => f.Name).IsUnique();
            entity.HasIndex(f => f.IsEnabled);
        });

        modelBuilder.Entity<FeatureFlagAuditLog>(entity =>
        {
            entity.ToTable("feature_flag_audit_logs");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(a => a.FeatureFlagId)
                .HasColumnName("feature_flag_id")
                .IsRequired();

            entity.Property(a => a.FeatureFlagName)
                .HasColumnName("feature_flag_name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(a => a.PreviousValue)
                .HasColumnName("previous_value")
                .IsRequired();

            entity.Property(a => a.NewValue)
                .HasColumnName("new_value")
                .IsRequired();

            entity.Property(a => a.ChangedAt)
                .HasColumnName("changed_at")
                .IsRequired();

            entity.Property(a => a.ChangedBy)
                .HasColumnName("changed_by")
                .HasMaxLength(200);

            entity.Property(a => a.Reason)
                .HasColumnName("reason")
                .HasMaxLength(1000);

            entity.Property(a => a.CorrelationId)
                .HasColumnName("correlation_id")
                .HasMaxLength(50);

            entity.Property(a => a.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(50);

            entity.Property(a => a.UserAgent)
                .HasColumnName("user_agent")
                .HasMaxLength(500);

            entity.HasIndex(a => a.FeatureFlagId);
            entity.HasIndex(a => a.FeatureFlagName);
            entity.HasIndex(a => a.ChangedAt);
            entity.HasIndex(a => a.ChangedBy);
            entity.HasIndex(a => a.CorrelationId);

            entity.HasOne(a => a.FeatureFlag)
                .WithMany()
                .HasForeignKey(a => a.FeatureFlagId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
