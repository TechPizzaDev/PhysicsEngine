using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace PhysicsEngine.Numerics;

public readonly struct Double2 : IEquatable<Double2>, ISpanFormattable
{
    private readonly Vector128<double> _value;

    public Double2(Vector128<double> value) => _value = value;

    public Double2(double x, double y) => _value = Vector128.Create(x, y);

    public Double2(double value) => _value = Vector128.Create(value);

    public Double2(ReadOnlySpan<double> values) => _value = Vector128.Create(values);

    public double X => _value.GetElement(0);

    public double Y => _value.GetElement(1);

    public Vector128<double> AsVector128() => _value;

    public void CopyTo(Span<double> destination) => _value.CopyTo(destination);

    public double Length() => Math.Sqrt(LengthSquared());

    public double LengthSquared() => Dot(this, this);

    public Double2 Normalize() => this / Length();

    public Double2 Transpose() => new(Transpose(_value));

    public Double2 RotateCW() => new(Transpose(_value) ^ Vector128.Create(-0.0, 0.0));

    public Double2 RotateCCW() => new(Transpose(_value) ^ Vector128.Create(0.0, -0.0));

    public Double2 Round() => new(Vector128.Round(_value));

    public Double2 Floor() => new(Vector128.Floor(_value));

    public Double2 Min(Double2 other) => new(Vector128.Min(_value, other._value));

    public Double2 Max(Double2 other) => new(Vector128.Max(_value, other._value));

    public Double2 Rotate(Double2 sinCos) => this * sinCos + RotateCW() * sinCos;

    public bool Equals(Double2 other) => _value == other._value;

    public override bool Equals(object? obj) => obj is Double2 other && Equals(other);

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => ToString("G", null);

    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, null);

    public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
    {
        var sb = new StringBuilder();
        sb.Append(this, format, formatProvider);
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

    public static double Dot(Double2 a, Double2 b) => Vector128.Dot(a._value, b._value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Cross(Double2 a, Double2 b)
    {
        Vector128<double> p = a._value * Transpose(b._value);
        return p.GetElement(1) - p.GetElement(0);
    }

    public static Double2 Floor(Double2 a) => new(Vector128.Floor(a._value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double2 MulAdd(Double2 a, Double2 b, Double2 c)
    {
        return new(Vector128.MultiplyAddEstimate(a._value, b._value, c._value));
    }

    public static Double2 SinCos(double angle)
    {
        (double sin, double cos) = Math.SinCos(angle);
        return new Double2(sin, cos);
    }

    private static Vector128<double> Transpose(Vector128<double> v) => Vector128.Shuffle(v, Vector128.Create(1, 0));

    public static bool operator ==(Double2 a, Double2 b) => a._value == b._value;
    public static bool operator !=(Double2 a, Double2 b) => a._value != b._value;

    public static Double2 operator +(Double2 a, Double2 b) => new(a._value + b._value);
    public static Double2 operator -(Double2 a, Double2 b) => new(a._value - b._value);
    public static Double2 operator -(Double2 a) => new(-a._value);

    public static Double2 operator *(Double2 a, Double2 b) => new(a._value * b._value);
    public static Double2 operator *(Double2 a, double b) => new(a._value * b);
    public static Double2 operator *(double a, Double2 b) => new(a * b._value);

    public static Double2 operator /(Double2 a, Double2 b) => new(a._value / b._value);
    public static Double2 operator /(Double2 a, double b) => new(a._value / b);

    public static implicit operator Double2(Vector2 vector) => new(Vector128.WidenLower(vector.AsVector128Unsafe()));

    public static explicit operator Vector2(Double2 vector) => Vector128.Narrow(vector._value, vector._value).AsVector2();
}
