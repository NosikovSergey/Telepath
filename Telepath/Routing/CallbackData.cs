using System.Diagnostics.CodeAnalysis;
using Telepath.Routing.Internal;

namespace Telepath.Routing;

public abstract class CallbackData
{
    public string Serialize() => CallbackDataSerializer.Serialize(this);

    public static T Deserialize<T>(string data) where T : CallbackData, new()
    {
        if (!TryDeserialize<T>(data, out var result))
            throw new ArgumentException($"Cannot deserialize '{data}' into '{typeof(T).Name}'.");
        return result;
    }

    public static bool TryDeserialize<T>(string data, [NotNullWhen(true)] out T? result)
        where T : CallbackData, new()
        => CallbackDataSerializer.TryDeserialize(data, out result);

    public static Func<string, bool> CreateFilter<T>() where T : CallbackData
        => CallbackDataSerializer.CreateFilter<T>();

    public static Func<string, bool> CreateFilter<T>(Func<T, bool> predicate) where T : CallbackData, new()
        => CallbackDataSerializer.CreateFilter(predicate);
}
