using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Api.Shutdown
{
    /// <summary>
    /// Service for handling graceful shutdown of the application.
    /// </summary>
    public class GracefulShutdownService : IHostedService
    {
        /// <summary>
        /// Starts the graceful shutdown service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        
        /// <summary>
        /// Stops the graceful shutdown service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Drain HTTP, consumers, outbox, connections
            return Task.CompletedTask;
        }
    }
}
