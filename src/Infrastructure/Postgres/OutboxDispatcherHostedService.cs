using System.Threading;
using System.Threading.Tasks;
using Infrastructure.Postgres.Outbox;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Postgres
{
    /// <summary>
    /// Hosted service for running the outbox dispatcher.
    /// </summary>
    public class OutboxDispatcherHostedService : BackgroundService
    {
        private readonly OutboxDispatcher _dispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutboxDispatcherHostedService"/> class.
        /// </summary>
        /// <param name="dispatcher">The outbox dispatcher.</param>
        public OutboxDispatcherHostedService(OutboxDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Executes the background service.
        /// </summary>
        /// <param name="stoppingToken">The stopping token.</param>
        /// <returns>A task representing the execution.</returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return _dispatcher.DispatchAsync(stoppingToken);
        }
    }
}