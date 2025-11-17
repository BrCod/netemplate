using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Interface for outbox message operations.
    /// </summary>
    public interface IOutboxRepository
    {
        /// <summary>
        /// Adds an outbox message asynchronously.
        /// </summary>
        /// <param name="message">The outbox message to add.</param>
        /// <returns>A task representing the operation.</returns>
        Task AddAsync(object message);
    }
}