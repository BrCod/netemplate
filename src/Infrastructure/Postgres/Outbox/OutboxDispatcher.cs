using System.Threading.Tasks;

namespace Infrastructure.Postgres.Outbox
{
    /// <summary>
    /// Handles the dispatching of outbox messages.
    /// </summary>
    public class OutboxDispatcher
    {
        /// <summary>
        /// Dispatches outbox messages asynchronously.
        /// </summary>
        /// <returns>A task representing the dispatch operation.</returns>
        public Task DispatchAsync()
        {
            // Poll outbox table, batch, retry, publish events
            return Task.CompletedTask;
        }
    }
}
