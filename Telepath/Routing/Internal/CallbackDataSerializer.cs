using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
            sb.Append(entry.Getter(instance) switch
            {
                string s           => EscapeString(s),
                double d           => d.ToString(CultureInfo.InvariantCulture),
                float f            => f.ToString(CultureInfo.InvariantCulture),
                decimal dc         => dc.ToString(CultureInfo.InvariantCulture),
                DateTimeOffset dto => dto.ToUnixTimeSeconds().ToString(),
                DateTime dt        => new DateTimeOffset(dt.ToUniversalTime(), TimeSpan.Zero).ToUnixTimeSeconds().ToString(),
                DateOnly d         => d.ToString("O", CultureInfo.InvariantCulture),
                TimeOnly t         => ((long)t.ToTimeSpan().TotalSeconds).ToString(),
                TimeSpan ts        => ((long)ts.TotalSeconds).ToString(),
                Enum e             => e.ToString("D"),
                var v              => v?.ToString() ?? ""
            });
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
                var colonIndex = IndexOfUnescapedColon(span);
                if (colonIndex < 0)
                {
                    result = null;
                    return false;
                }
                segment = span[..colonIndex];
                span = span[(colonIndex + 1)..];
            }

            if (!TryConvertValue(segment, entries[i].PropertyType, entries[i].IsNullableReference, out var converted))
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

        return hasFields
            ? data => data.StartsWith(prefix + ":", StringComparison.Ordinal)
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

    private static bool TryConvertValue(ReadOnlySpan<char> value, Type targetType, bool isNullableReference, out object? result)
    {
        var underlying = Nullable.GetUnderlyingType(targetType);
        var type = underlying ?? targetType;

        if ((underlying != null || isNullableReference) && value.IsEmpty)
        {
            result = null;
            return true;
        }

        if (type == typeof(string))         { result = Unescape(value); return true; }
        if (type == typeof(bool))           { var ok = bool.TryParse(value, out var v);     result = v; return ok; }
        if (type == typeof(byte))           { var ok = byte.TryParse(value, out var v);     result = v; return ok; }
        if (type == typeof(sbyte))          { var ok = sbyte.TryParse(value, out var v);    result = v; return ok; }
        if (type == typeof(short))          { var ok = short.TryParse(value, out var v);    result = v; return ok; }
        if (type == typeof(ushort))         { var ok = ushort.TryParse(value, out var v);   result = v; return ok; }
        if (type == typeof(int))            { var ok = int.TryParse(value, out var v);      result = v; return ok; }
        if (type == typeof(uint))           { var ok = uint.TryParse(value, out var v);     result = v; return ok; }
        if (type == typeof(long))           { var ok = long.TryParse(value, out var v);     result = v; return ok; }
        if (type == typeof(ulong))          { var ok = ulong.TryParse(value, out var v);    result = v; return ok; }
        if (type == typeof(float))          { var ok = float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v);   result = v; return ok; }
        if (type == typeof(double))         { var ok = double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v);  result = v; return ok; }
        if (type == typeof(decimal))        { var ok = decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v); result = v; return ok; }
        if (type == typeof(Guid))           { var ok = Guid.TryParse(value, out var v);     result = v; return ok; }
        if (type == typeof(DateTimeOffset)) { var ok = long.TryParse(value, out var v);     result = DateTimeOffset.FromUnixTimeSeconds(v);             return ok; }
        if (type == typeof(DateTime))       { var ok = long.TryParse(value, out var v);     result = DateTimeOffset.FromUnixTimeSeconds(v).UtcDateTime; return ok; }
        if (type == typeof(DateOnly))       { var ok = DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var v);          result = v; return ok; }
        if (type == typeof(TimeOnly))       { var ok = long.TryParse(value, out var v);     result = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(v));    return ok; }
        if (type == typeof(TimeSpan))       { var ok = long.TryParse(value, out var v);     result = TimeSpan.FromSeconds(v);                          return ok; }

        if (type.IsEnum)
        {
            if (!TryConvertValue(value, Enum.GetUnderlyingType(type), false, out var num)) { result = null; return false; }
            result = Enum.ToObject(type, num!);
            return true;
        }

        result = null;
        return false;
    }

    private static string EscapeString(string s)
    {
        if (s.Length == 0 || (s.IndexOf('\\') < 0 && s.IndexOf(':') < 0)) return s;
        return s.Replace("\\", "\\\\").Replace(":", "\\:");
    }

    private static string Unescape(ReadOnlySpan<char> span)
    {
        if (span.IndexOf('\\') < 0) return span.ToString();

        var sb = new StringBuilder(span.Length);
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == '\\' && i + 1 < span.Length)
                sb.Append(span[++i]);
            else
                sb.Append(span[i]);
        }
        return sb.ToString();
    }

    private static int IndexOfUnescapedColon(ReadOnlySpan<char> span)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == '\\') { i++; continue; }
            if (span[i] == ':') return i;
        }
        return -1;
    }
}
