using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Unicode;

namespace PhysicsEngine.Memory;

public readonly ref struct Utf8Span : IUtf8SpanFormattable, ISpanFormattable, IEquatable<Utf8Span>
{
    private readonly ReadOnlySpan<byte> _span;

    public ReadOnlySpan<byte> Span => _span;

    public Utf8Span(ReadOnlySpan<byte> span) => _span = span;

    public bool Equals(Utf8Span other) => _span.SequenceEqual(other);

    public override bool Equals([NotNullWhen(true)] object? obj) => throw new NotImplementedException();

    public override int GetHashCode()
    {
        var code = new HashCode();
        code.AddBytes(_span);
        return code.ToHashCode();
    }

    public override string ToString() => Encoding.UTF8.GetString(_span);

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length <= _span.Length)
        {
            _span.CopyTo(utf8Destination);
            bytesWritten = _span.Length;
            return true;
        }
        bytesWritten = 0;
        return false;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return Utf8.ToUtf16(_span, destination, out _, out charsWritten) == OperationStatus.Done;
    }

    public static bool operator ==(Utf8Span a, Utf8Span b) => a.Equals(b);

    public static bool operator !=(Utf8Span a, Utf8Span b) => !a.Equals(b);

    // TODO: do not ToString

    public static string operator +(Utf8Span a, string b) => $"{a.ToString()}{b}";

    public static string operator +(string a, Utf8Span b) => $"{a}{b.ToString()}";

    public static implicit operator ReadOnlySpan<byte>(Utf8Span span) => span._span;
}
