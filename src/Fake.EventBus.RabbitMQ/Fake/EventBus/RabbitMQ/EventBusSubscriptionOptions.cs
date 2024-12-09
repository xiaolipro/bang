using System.Text.Json.Serialization.Metadata;

namespace Fake.EventBus.RabbitMQ;

public class EventBusSubscriptionOptions
{
    /// <summary>
    /// event name: event type, for keyed services
    /// </summary>
    public Dictionary<string, Type> EventTypes { get; } = [];

    public JsonSerializerOptions JsonSerializerOptions { get; } = new(DefaultSerializerOptions);

    internal static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault ? CreateDefaultTypeResolver() : JsonTypeInfoResolver.Combine()
    };
    
    private static IJsonTypeInfoResolver CreateDefaultTypeResolver() => new DefaultJsonTypeInfoResolver();
}