using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Netemplate.Application.Services;
using Netemplate.Infrastructure.FeatureFlags.Services;

namespace Netemplate.Infrastructure.FeatureFlags;

/// <summary>
/// Extension methods for configuring feature flags infrastructure
/// </summary>
public static class FeatureFlagsServiceExtensions
{
    /// <summary>
    /// Registers feature flag services with audit trail persistence
    /// </summary>
    public static IServiceCollection AddFeatureFlagsPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string not found");

        services.AddDbContext<FeatureFlagsDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register feature flag service (replaces in-memory implementation)
        services.AddScoped<IFeatureFlagService, PersistentFeatureFlagService>();

        // Memory cache is typically already registered, but ensure it's available
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Ensures feature flags database is created and migrated
    /// </summary>
    public static async Task InitializeFeatureFlagsDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FeatureFlagsDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
