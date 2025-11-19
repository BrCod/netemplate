using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Netemplate.Application.Interfaces;
using Netemplate.Infrastructure.Persistence.Postgres.Outbox;
using System.Text.Json;

namespace Netemplate.Infrastructure.Persistence.Postgres;

public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);

    public OutboxDispatcher(IServiceProvider serviceProvider, ILogger<OutboxDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox dispatcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox dispatcher stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        foreach (var message in pendingMessages)
        {
            try
            {
                await messageBus.PublishAsync(message.EventType, message.Payload, ct);
                
                message.ProcessedAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync(ct);

                _logger.LogInformation("Outbox message {MessageId} processed", message.Id);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                await dbContext.SaveChangesAsync(ct);

                _logger.LogWarning(ex, "Failed to process outbox message {MessageId}, retry {RetryCount}", 
                    message.Id, message.RetryCount);
            }
        }
    }
}
