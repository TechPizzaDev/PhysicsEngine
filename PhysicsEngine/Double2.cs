using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;

namespace PhysicsEngine;

public readonly struct Double2(Vector128<double> value) : ISpanFormattable
{
    private readonly Vector128<double> _value = value;

    public Double2(double x, double y) : this(Vector128.Create(x, y))
    {
    }

    public double X => _value.GetElement(0);
    public double Y => _value.GetElement(1);

    public override string ToString()
    {
        return ToString("G", null);
    }

    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
    {
        return ToString(format, null);
    }

    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
    {
        var sb = new StringBuilder();
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

        sb.Append('<');
        sb.Append(X.ToString(format, formatProvider));
        sb.Append(separator);
        sb.Append(' ');
        sb.Append(Y.ToString(format, formatProvider));
        sb.Append('>');

        return sb.ToString();
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        string separator = NumberFormatInfo.GetInstance(provider).NumberGroupSeparator;
        Span<char> dst = destination;

        if (!"<".TryCopyTo(dst))
            goto Fail;
        dst = dst[1..];

        if (!X.TryFormat(dst, out int written, format, provider))
            goto Fail;
        dst = dst[written..];

        if (!separator.TryCopyTo(dst))
            goto Fail;
        dst = dst[separator.Length..];
        
        if (!" ".TryCopyTo(dst))
            goto Fail;
        dst = dst[1..];

        if (!Y.TryFormat(dst, out written, format, provider))
            goto Fail;
        dst = dst[written..];
        
        if (!">".TryCopyTo(dst))
            goto Fail;
        dst = dst[1..];

        charsWritten = destination.Length - dst.Length;
        return true;

    Fail:
        charsWritten = 0;
        return false;
    }

    public static double Cross(Double2 a, Double2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }

    public static Double2 Floor(Double2 a) => new(Vector128.Floor(a._value));

    public static Double2 operator +(Double2 a, Double2 b) => new(a._value + b._value);
    public static Double2 operator -(Double2 a, Double2 b) => new(a._value - b._value);

    public static Double2 operator *(Double2 a, Double2 b) => new(a._value * b._value);
    public static Double2 operator *(Double2 a, double b) => new(a._value * b);
    public static Double2 operator *(double a, Double2 b) => new(a * b._value);

    public static Double2 operator /(Double2 a, Double2 b) => new(a._value / b._value);
    public static Double2 operator /(Double2 a, double b) => new(a._value / b);

    public static implicit operator Double2(Vector2 vector) => new(Vector128.WidenLower(vector.AsVector128()));

    public static explicit operator Vector2(Double2 vector) => Vector128.Narrow(vector._value, default).AsVector2();
}
