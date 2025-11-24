using Netemplate.Application.Interfaces;

namespace Netemplate.Api.IntegrationTests;

/// <summary>
/// In-memory implementation of IEventPublisher for testing.
/// </summary>
public sealed class InMemoryEventPublisher : IEventPublisher
{
    private readonly List<object> _publishedEvents = new();

    public IReadOnlyList<object> PublishedEvents => _publishedEvents.AsReadOnly();

    public Task PublishAsync<T>(T @event, CancellationToken ct = default)
    {
        if (@event != null)
        {
            _publishedEvents.Add(@event);
        }
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _publishedEvents.Clear();
    }
}
