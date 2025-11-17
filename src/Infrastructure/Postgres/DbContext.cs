using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Postgres
{
    /// <summary>
    /// Represents the application's database context.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Gets or sets the products in the database.
        /// </summary>
        public DbSet<Product> Products { get; set; }

        /// <summary>
        /// Gets or sets the outbox messages in the database.
        /// </summary>
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppDbContext"/> class.
        /// </summary>
        /// <param name="options">The options to configure the database context.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        /// <summary>
        /// Configures the model for the database context.
        /// </summary>
        /// <param name="modelBuilder">The model builder to configure.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().HasKey(p => p.Id);
            modelBuilder.Entity<Product>().Property(p => p.Name).IsRequired().HasMaxLength(200);
            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OutboxMessage>().HasKey(o => o.Id);
            modelBuilder.Entity<OutboxMessage>().Property(o => o.EventType).IsRequired().HasMaxLength(200);
            modelBuilder.Entity<OutboxMessage>().Property(o => o.Payload).IsRequired();
            modelBuilder.Entity<OutboxMessage>().Property(o => o.CreatedAt).IsRequired();
            modelBuilder.Entity<OutboxMessage>().Property(o => o.RetryCount).HasDefaultValue(0);
            modelBuilder.Entity<OutboxMessage>().HasIndex(o => o.CreatedAt);
            modelBuilder.Entity<OutboxMessage>().HasIndex(o => new { o.ProcessedAt, o.CreatedAt });
        }
    }
}
