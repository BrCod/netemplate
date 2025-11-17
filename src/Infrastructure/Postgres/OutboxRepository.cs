using System.Threading.Tasks;
using Application.Interfaces;

namespace Infrastructure.Postgres
{
    /// <summary>
    /// Repository for outbox message operations.
    /// </summary>
    public class OutboxRepository : IOutboxRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutboxRepository"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public OutboxRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds an outbox message asynchronously.
        /// </summary>
        /// <param name="message">The event message to add to the outbox.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task AddAsync(object message)
        {
            var outboxMessage = OutboxMessage.Create(message);
            await _context.OutboxMessages.AddAsync(outboxMessage);
        }
    }
}