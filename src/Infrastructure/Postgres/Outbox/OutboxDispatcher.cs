using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Postgres.Outbox
{
    /// <summary>
    /// Handles the dispatching of outbox messages.
    /// </summary>
    public class OutboxDispatcher
    {
        private readonly AppDbContext _context;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<OutboxDispatcher> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
        private readonly int _maxRetryCount = 3;
        private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Initializes a new instance of the <see cref="OutboxDispatcher"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="logger">The logger.</param>
        public OutboxDispatcher(AppDbContext context, IEventPublisher eventPublisher, ILogger<OutboxDispatcher> logger)
        {
            _context = context;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        /// <summary>
        /// Dispatches outbox messages asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the dispatch operation.</returns>
        public async Task DispatchAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingMessagesAsync(cancellationToken);
                    await Task.Delay(_pollingInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Outbox dispatcher cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in outbox dispatcher");
                    await Task.Delay(_retryDelay, cancellationToken);
                }
            }
        }

        private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
        {
            var pendingMessages = await _context.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.CreatedAt)
                .Take(10) // Process in batches
                .ToListAsync(cancellationToken);

            foreach (var message in pendingMessages)
            {
                try
                {
                    var @event = message.GetEvent<object>();
                    if (@event == null)
                    {
                        _logger.LogError("Failed to deserialize event from outbox message {MessageId}", message.Id);
                        message.ProcessedAt = DateTime.UtcNow;
                        message.Error = "Deserialization failed";
                        continue;
                    }

                    await _eventPublisher.PublishEventAsync(@event);
                    message.ProcessedAt = DateTime.UtcNow;
                    message.Error = null;
                    _logger.LogInformation("Successfully processed outbox message {MessageId} of type {EventType}", message.Id, message.EventType);
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    message.Error = ex.Message;

                    if (message.RetryCount >= _maxRetryCount)
                    {
                        _logger.LogError(ex, "Failed to process outbox message {MessageId} after {RetryCount} attempts", message.Id, message.RetryCount);
                        // Mark as processed to prevent infinite retries, but log the error
                        message.ProcessedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _logger.LogWarning(ex, "Failed to process outbox message {MessageId}, retry {RetryCount}/{MaxRetries}", message.Id, message.RetryCount, _maxRetryCount);
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
