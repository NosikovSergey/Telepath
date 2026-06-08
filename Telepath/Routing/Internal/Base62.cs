using System.Numerics;
using System.Text;

namespace Telepath.Routing.Internal;

internal static class Base62
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private static readonly sbyte[] DecodeTable = BuildDecodeTable();

    private static sbyte[] BuildDecodeTable()
    {
        var table = new sbyte[128];
        Array.Fill(table, (sbyte)-1);
        for (var i = 0; i < Alphabet.Length; i++)
            table[Alphabet[i]] = (sbyte)i;
        return table;
    }

    internal static void AppendTo(StringBuilder sb, ulong value)
    {
        if (value == 0) { sb.Append('0'); return; }
        Span<char> buf = stackalloc char[11]; // ulong.MaxValue fits in 11 base-62 digits
        var pos = 11;
        while (value > 0)
        {
            buf[--pos] = Alphabet[(int)(value % 62)];
            value /= 62;
        }
        sb.Append(buf[pos..]);
    }

    internal static bool TryDecode(ReadOnlySpan<char> s, out ulong value)
    {
        value = 0;
        if (s.IsEmpty) return false;
        foreach (var c in s)
        {
            if (c >= 128 || DecodeTable[c] < 0) return false;
            var digit = (ulong)DecodeTable[c];
            if (value > (ulong.MaxValue - digit) / 62) return false; // overflow guard
            value = value * 62 + digit;
        }
        return true;
    }

    internal static void AppendTo(StringBuilder sb, BigInteger value)
    {
        if (value.IsZero) { sb.Append('0'); return; }
        Span<char> buf = stackalloc char[22]; // 128-bit value fits in 22 base-62 digits
        var pos = 22;
        while (value > 0)
        {
            value = BigInteger.DivRem(value, 62, out var rem);
            buf[--pos] = Alphabet[(int)rem];
        }
        sb.Append(buf[pos..]);
    }

    internal static bool TryDecode(ReadOnlySpan<char> s, out BigInteger value)
    {
        value = BigInteger.Zero;
        if (s.IsEmpty) return false;
        foreach (var c in s)
        {
            if (c >= 128 || DecodeTable[c] < 0) return false;
            value = value * 62 + DecodeTable[c];
        }
        return true;
    }
}
