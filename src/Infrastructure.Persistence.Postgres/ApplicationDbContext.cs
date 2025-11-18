using Microsoft.EntityFrameworkCore;
using Netemplate.Domain.Entities;
using Netemplate.Infrastructure.Persistence.Postgres.Outbox;

namespace Netemplate.Infrastructure.Persistence.Postgres;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(p => p.Name)
                .HasConversion(
                    v => v.Value,
                    v => Domain.ValueObjects.ProductName.Create(v))
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(p => p.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            entity.OwnsOne(p => p.Price, price =>
            {
                price.Property(m => m.Amount)
                    .HasColumnName("price_amount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                price.Property(m => m.Currency)
                    .HasColumnName("price_currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            entity.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(p => p.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            entity.Property(p => p.IsActive)
                .HasColumnName("is_active")
                .IsRequired();

            entity.Ignore(p => p.DomainEvents);

            entity.HasIndex(p => p.Name).IsUnique();
        });

        modelBuilder.ConfigureOutbox();
    }
}
