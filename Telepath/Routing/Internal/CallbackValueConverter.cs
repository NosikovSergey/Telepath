using System.Globalization;
using System.Numerics;
using System.Text;

namespace Telepath.Routing.Internal;

internal static class CallbackValueConverter
{
    internal static void AppendTo(StringBuilder sb, object? value)
    {
        switch (value)
        {
            case char ch:            Base62.AppendTo(sb, (ulong)ch); break;
            case bool b:             sb.Append(b ? '1' : '0'); break;
            case byte by:            Base62.AppendTo(sb, (ulong)by); break;
            case sbyte sv:           Base62.AppendTo(sb, (ulong)(byte)sv); break;
            case short s:            Base62.AppendTo(sb, (ulong)(ushort)s); break;
            case ushort us:          Base62.AppendTo(sb, (ulong)us); break;
            case int i:              Base62.AppendTo(sb, (ulong)(uint)i); break;
            case uint ui:            Base62.AppendTo(sb, (ulong)ui); break;
            case long l:             Base62.AppendTo(sb, (ulong)l); break;
            case ulong ul:           Base62.AppendTo(sb, ul); break;
            case string str:         AppendEscaped(sb, str); break;
            case float f:            AppendSpanFormattable(sb, f); break;
            case double d:           AppendSpanFormattable(sb, d); break;
            case decimal dc:         AppendSpanFormattable(sb, dc); break;
            case Guid g:             Base62.AppendTo(sb, new BigInteger(g.ToByteArray(), isUnsigned: true)); break;
            case DateTimeOffset dto: Base62.AppendTo(sb, (ulong)dto.ToUnixTimeSeconds()); break;
            case DateTime dt:        Base62.AppendTo(sb, (ulong)new DateTimeOffset(dt.ToUniversalTime(), TimeSpan.Zero).ToUnixTimeSeconds()); break;
            case DateOnly d:         Base62.AppendTo(sb, (ulong)d.DayNumber); break;
            case TimeOnly t:         Base62.AppendTo(sb, (ulong)(long)t.ToTimeSpan().TotalSeconds); break;
            case TimeSpan ts:        Base62.AppendTo(sb, (ulong)(long)ts.TotalSeconds); break;
            case Enum e:             Base62.AppendTo(sb, EnumToUlong(e)); break;
            default:                 sb.Append(value?.ToString()); break;
        }
    }

    internal static bool TryDeserialize(ReadOnlySpan<char> raw, Type targetType, bool isNullableReference, out object? result)
    {
        var underlying = Nullable.GetUnderlyingType(targetType);
        var type = underlying ?? targetType;

        if ((underlying != null || isNullableReference) && raw.IsEmpty)
        {
            result = null;
            return true;
        }

        // Types that don't use Base62 ulong encoding
        if (type == typeof(string))  { result = Unescape(raw); return true; }
        if (type == typeof(bool))    return ParseBool(raw, out result);
        if (type == typeof(float))   { var ok = float.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v);   result = v; return ok; }
        if (type == typeof(double))  { var ok = double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v);  result = v; return ok; }
        if (type == typeof(decimal)) { var ok = decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v); result = v; return ok; }
        if (type == typeof(Guid))    return ParseGuid(raw, out result);

        // Enum piggybacks on its underlying numeric type
        if (type.IsEnum)
        {
            if (!TryDeserialize(raw, Enum.GetUnderlyingType(type), false, out var num)) { result = null; return false; }
            result = Enum.ToObject(type, num!);
            return true;
        }

        // All remaining types are Base62-encoded ulong — decode once
        if (!Base62.TryDecode(raw, out ulong u)) { result = null; return false; }

        if (type == typeof(char))           { if (u > char.MaxValue)                      { result = null; return false; } result = (char)u;            return true; }
        if (type == typeof(byte))           { if (u > byte.MaxValue)                      { result = null; return false; } result = (byte)u;            return true; }
        if (type == typeof(sbyte))          { if (u > byte.MaxValue)                      { result = null; return false; } result = (sbyte)(byte)u;     return true; }
        if (type == typeof(short))          { if (u > ushort.MaxValue)                    { result = null; return false; } result = (short)(ushort)u;   return true; }
        if (type == typeof(ushort))         { if (u > ushort.MaxValue)                    { result = null; return false; } result = (ushort)u;           return true; }
        if (type == typeof(int))            { if (u > uint.MaxValue)                      { result = null; return false; } result = (int)(uint)u;        return true; }
        if (type == typeof(uint))           { if (u > uint.MaxValue)                      { result = null; return false; } result = (uint)u;             return true; }
        if (type == typeof(long))           { result = (long)u;                                                                                          return true; }
        if (type == typeof(ulong))          { result = u;                                                                                                return true; }
        if (type == typeof(DateTimeOffset)) { result = DateTimeOffset.FromUnixTimeSeconds((long)u);              return true; }
        if (type == typeof(DateTime))       { result = DateTimeOffset.FromUnixTimeSeconds((long)u).UtcDateTime;  return true; }
        if (type == typeof(DateOnly))       { if (u > (ulong)DateOnly.MaxValue.DayNumber) { result = null; return false; } result = DateOnly.FromDayNumber((int)u); return true; }
        if (type == typeof(TimeOnly))       { result = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds((long)u));     return true; }
        if (type == typeof(TimeSpan))       { result = TimeSpan.FromSeconds((long)u);                           return true; }

        result = null;
        return false;
    }

    internal static int IndexOfUnescapedColon(ReadOnlySpan<char> span)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == '\\') { i++; continue; }
            if (span[i] == ':') return i;
        }
        return -1;
    }

    private static bool ParseBool(ReadOnlySpan<char> raw, out object? result)
    {
        if (raw.Length == 1 && raw[0] == '1') { result = true;  return true; }
        if (raw.Length == 1 && raw[0] == '0') { result = false; return true; }
        result = null;
        return false;
    }

    private static bool ParseGuid(ReadOnlySpan<char> raw, out object? result)
    {
        if (!Base62.TryDecode(raw, out BigInteger big) || big.GetByteCount(isUnsigned: true) > 16)
        {
            result = null;
            return false;
        }
        Span<byte> guidBytes = stackalloc byte[16];
        big.TryWriteBytes(guidBytes, out _, isUnsigned: true);
        result = new Guid(guidBytes);
        return true;
    }

    private static ulong EnumToUlong(Enum e) => Enum.GetUnderlyingType(e.GetType()) switch
    {
        var t when t == typeof(ulong)  => Convert.ToUInt64(e),
        var t when t == typeof(long)   => (ulong)Convert.ToInt64(e),
        var t when t == typeof(uint)   => Convert.ToUInt32(e),
        var t when t == typeof(int)    => (ulong)(uint)Convert.ToInt32(e),
        var t when t == typeof(ushort) => Convert.ToUInt16(e),
        var t when t == typeof(short)  => (ulong)(ushort)Convert.ToInt16(e),
        var t when t == typeof(byte)   => Convert.ToByte(e),
        var t when t == typeof(sbyte)  => (ulong)(byte)Convert.ToSByte(e),
        _                              => (ulong)Convert.ToInt64(e),
    };

    private static void AppendEscaped(StringBuilder sb, string s)
    {
        if (s.Length == 0) return;
        if (s.IndexOf('\\') < 0 && s.IndexOf(':') < 0) { sb.Append(s); return; }
        foreach (var c in s)
        {
            if (c == '\\' || c == ':') sb.Append('\\');
            sb.Append(c);
        }
    }

    private static void AppendSpanFormattable<T>(StringBuilder sb, T value) where T : ISpanFormattable
    {
        Span<char> buf = stackalloc char[64];
        value.TryFormat(buf, out var len, default, CultureInfo.InvariantCulture);
        sb.Append(buf[..len]);
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
}
