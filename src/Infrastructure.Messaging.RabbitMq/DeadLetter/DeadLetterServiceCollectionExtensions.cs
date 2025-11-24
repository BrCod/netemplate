using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Netemplate.Infrastructure.Messaging.RabbitMq.DeadLetter;

/// <summary>
/// Extension methods for registering dead-letter queue services
/// </summary>
public static class DeadLetterServiceCollectionExtensions
{
    /// <summary>
    /// Add dead-letter queue handling and monitoring services
    /// </summary>
    public static IServiceCollection AddDeadLetterQueue(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind DLQ options from configuration
        var dlqOptions = new DeadLetterQueueOptions();
        configuration.GetSection(DeadLetterQueueOptions.SectionName).Bind(dlqOptions);
        services.AddSingleton(dlqOptions);

        // Register alert service
        services.AddSingleton<IDeadLetterAlertService, LoggingDeadLetterAlertService>();

        // Register DLQ handler
        services.AddSingleton<DeadLetterQueueHandler>(sp =>
        {
            var connectionFactory = sp.GetRequiredService<IConnectionFactory>();
            var alertService = sp.GetRequiredService<IDeadLetterAlertService>();
            var logger = sp.GetRequiredService<ILogger<DeadLetterQueueHandler>>();
            
            return new DeadLetterQueueHandler(connectionFactory, alertService, logger, dlqOptions);
        });

        return services;
    }

    /// <summary>
    /// Initialize dead-letter queue infrastructure (call during application startup)
    /// </summary>
    public static async Task InitializeDeadLetterQueueAsync(
        this IServiceProvider serviceProvider,
        CancellationToken ct = default)
    {
        var handler = serviceProvider.GetRequiredService<DeadLetterQueueHandler>();
        var options = serviceProvider.GetRequiredService<DeadLetterQueueOptions>();

        await handler.SetupDeadLetterInfrastructureAsync(ct);

        if (options.EnableMonitoring)
        {
            await handler.StartMonitoringAsync(ct);
        }
    }
}
