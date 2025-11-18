using System.Collections.Concurrent;

namespace Netemplate.Application.Messaging.SchemaRegistry;

public sealed class InMemoryEventSchemaRegistry : IEventSchemaRegistry
{
    private readonly ConcurrentDictionary<string, SortedDictionary<int, string>> _schemas = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string eventType, int version, string schema)
    {
        var map = _schemas.GetOrAdd(eventType, _ => new SortedDictionary<int, string>());
        map[version] = schema;
    }

    public (int Version, string Schema)? GetLatest(string eventType)
    {
        if (!_schemas.TryGetValue(eventType, out var map) || map.Count == 0)
            return null;
        var latest = map.Last();
        return (latest.Key, latest.Value);
    }

    public string? Get(string eventType, int version)
    {
        if (_schemas.TryGetValue(eventType, out var map) && map.TryGetValue(version, out var schema))
            return schema;
        return null;
    }
}
