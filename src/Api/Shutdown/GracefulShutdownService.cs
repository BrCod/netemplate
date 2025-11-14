using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Api.Shutdown
{
    public class GracefulShutdownService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Drain HTTP, consumers, outbox, connections
            return Task.CompletedTask;
        }
    }
}
