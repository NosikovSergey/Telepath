using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Telepath.Routing.Internal;

internal static class CallbackDataSerializer
{
    private readonly record struct PropertyEntry(
        Type PropertyType,
        bool IsNullableReference,
        Func<CallbackData, object?> Getter,
        Action<CallbackData, object?> Setter);

    private static readonly ConcurrentDictionary<Type, string> PrefixCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyEntry[]> PropertiesCache = new();

    internal static string Serialize(CallbackData instance)
    {
        var type = instance.GetType();
        var prefix = GetPrefix(type);
        var entries = GetOrderedProperties(type);

        if (entries.Length == 0)
            return prefix;

        var sb = new StringBuilder(prefix);
        foreach (var entry in entries)
        {
            sb.Append(':');
            CallbackValueConverter.AppendTo(sb, entry.Getter(instance));
        }
        return sb.ToString();
    }

    internal static bool TryDeserialize<T>(string data, [NotNullWhen(true)] out T? result)
        where T : CallbackData, new()
    {
        var prefix = GetPrefix(typeof(T));
        var entries = GetOrderedProperties(typeof(T));

        ReadOnlySpan<char> span = data;

        if (!span.StartsWith(prefix.AsSpan(), StringComparison.Ordinal))
        {
            result = null;
            return false;
        }

        span = span[prefix.Length..];

        if (entries.Length == 0)
        {
            result = span.IsEmpty ? new T() : null;
            return result is not null;
        }

        if (span.IsEmpty || span[0] != ':')
        {
            result = null;
            return false;
        }

        span = span[1..];

        var instance = new T();
        for (var i = 0; i < entries.Length; i++)
        {
            ReadOnlySpan<char> segment;

            if (i == entries.Length - 1)
            {
                segment = span;
                span = ReadOnlySpan<char>.Empty;
            }
            else
            {
                var colonIndex = CallbackValueConverter.IndexOfUnescapedColon(span);
                if (colonIndex < 0)
                {
                    result = null;
                    return false;
                }
                segment = span[..colonIndex];
                span = span[(colonIndex + 1)..];
            }

            if (!CallbackValueConverter.TryDeserialize(segment, entries[i].PropertyType, entries[i].IsNullableReference, out var converted))
            {
                result = null;
                return false;
            }
            entries[i].Setter(instance, converted);
        }

        if (!span.IsEmpty)
        {
            result = null;
            return false;
        }

        result = instance;
        return true;
    }

    internal static Func<string, bool> CreateFilter<T>() where T : CallbackData
    {
        var prefix = GetPrefix(typeof(T));
        var hasFields = GetOrderedProperties(typeof(T)).Length > 0;

        var prefixWithColon = prefix + ":";
        return hasFields
            ? data => data.StartsWith(prefixWithColon, StringComparison.Ordinal)
            : data => data == prefix;
    }

    internal static Func<string, bool> CreateFilter<T>(Func<T, bool> predicate) where T : CallbackData, new()
    {
        var baseFilter = CreateFilter<T>();
        return data => baseFilter(data) && CallbackData.TryDeserialize<T>(data, out var parsed) && predicate(parsed);
    }

    private static string GetPrefix(Type type) =>
        PrefixCache.GetOrAdd(type, t =>
            t.GetCustomAttribute<CallbackPrefixAttribute>()?.Prefix
            ?? throw new InvalidOperationException(
                $"Type '{t.Name}' must have a [CallbackPrefix(\"...\")] attribute."));

    private static PropertyEntry[] GetOrderedProperties(Type type) =>
        PropertiesCache.GetOrAdd(type, t =>
        {
            var context = new NullabilityInfoContext();
            return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.DeclaringType != typeof(CallbackData))
                .OrderBy(p => p.MetadataToken)
                .Select(p => new PropertyEntry(
                    p.PropertyType,
                    !p.PropertyType.IsValueType && context.Create(p).WriteState == NullabilityState.Nullable,
                    CompileGetter(p),
                    CompileSetter(p)))
                .ToArray();
        });

    private static Func<CallbackData, object?> CompileGetter(PropertyInfo prop)
    {
        var param = Expression.Parameter(typeof(CallbackData), "instance");
        var cast = Expression.Convert(param, prop.DeclaringType!);
        var property = Expression.Property(cast, prop);
        var box = Expression.Convert(property, typeof(object));
        return Expression.Lambda<Func<CallbackData, object?>>(box, param).Compile();
    }

    private static Action<CallbackData, object?> CompileSetter(PropertyInfo prop)
    {
        var instanceParam = Expression.Parameter(typeof(CallbackData), "instance");
        var valueParam = Expression.Parameter(typeof(object), "value");
        var cast = Expression.Convert(instanceParam, prop.DeclaringType!);
        var property = Expression.Property(cast, prop);
        var unbox = Expression.Convert(valueParam, prop.PropertyType);
        var assign = Expression.Assign(property, unbox);
        return Expression.Lambda<Action<CallbackData, object?>>(assign, instanceParam, valueParam).Compile();
    }
}
