namespace Netemplate.Application.Messaging.SchemaRegistry;

public interface IEventSchemaRegistry
{
    // Registers a schema for a given event type and version
    void Register(string eventType, int version, string schema);

    // Retrieves the latest schema version for an event type
    (int Version, string Schema)? GetLatest(string eventType);

    // Retrieves a specific schema version for an event type
    string? Get(string eventType, int version);
}
