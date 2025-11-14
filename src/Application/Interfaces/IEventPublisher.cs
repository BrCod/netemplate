using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Interface for publishing domain events.
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>Publish a domain event.</summary>
        Task PublishEventAsync(object @event);
    }
}
