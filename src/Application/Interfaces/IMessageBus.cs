using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Interface for message bus publish/subscribe operations.
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>Publish a message to the bus.</summary>
        Task PublishAsync(object message);
        /// <summary>Subscribe to a topic with a handler.</summary>
        Task SubscribeAsync<T>(string topic, Func<T, Task> handler);
    }
}
