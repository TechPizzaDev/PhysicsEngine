using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using MonoGame.Framework;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Numerics;

public readonly struct Bound2 : IEquatable<Bound2>, ISpanFormattable, IShape2D
{
    public readonly Double2 Min;
    public readonly Double2 Max;

    public readonly Double2 Position => Min;

    public readonly Double2 Size => Max - Min;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bound2(Double2 min, Double2 max)
    {
        Min = min;
        Max = max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bound2(RectangleF rect)
    {
        (Vector128<double> min, Vector128<double> max) = Vector128.Widen(rect.AsVector4().AddLowerToUpper().AsVector128());
        Min = new Double2(min);
        Max = new Double2(max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetArea()
    {
        Vector128<double> size = Max.AsVector128() - Min.AsVector128();
        return size.GetElement(1) * size.GetElement(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Double2 GetCenter()
    {
        return Position + Size / 2;
    }

    Bound2 IShape2D.GetBounds() => this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector256<double> AsVector256() => Vector256.Create(Min.AsVector128(), Max.AsVector128());
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Double2 ClosestTo(Double2 point) => Min + (point - Min).Clamp(Double2.Zero, Size);

    public bool Equals(Bound2 other) => AsVector256() == other.AsVector256();

    public Bound2 WithPosition(Double2 position) => new(position, position + Size);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RectangleF ToRectF()
    {
        Vector128<double> min = Min.AsVector128();
        return new RectangleF(Vector128.Narrow(min, Max.AsVector128() - min));
    }

    public bool HasArea() => Vector128.LessThanAll(Min.AsVector128(), Max.AsVector128());

    public Bound2 Intersect(Bound2 bound) => new(Min.Max(bound.Min), Max.Min(bound.Max));

    public override bool Equals(object? obj) => obj is Bound2 other && Equals(other);

    public override int GetHashCode() => AsVector256().GetHashCode();

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

        if (!"{Max:".TryCopyTo(dst))
            goto Fail;
        dst = dst[1..];

        if (!Min.TryFormat(dst, out int written, format, provider))
            goto Fail;
        dst = dst[written..];

        if (!separator.TryCopyTo(dst))
            goto Fail;
        dst = dst[separator.Length..];

        if (!" Min:".TryCopyTo(dst))
            goto Fail;
        dst = dst[1..];

        if (!Max.TryFormat(dst, out written, format, provider))
            goto Fail;
        dst = dst[written..];

        if (!"}".TryCopyTo(dst))
            goto Fail;
        dst = dst[1..];

        charsWritten = destination.Length - dst.Length;
        return true;

    Fail:
        charsWritten = 0;
        return false;
    }
    public static bool operator ==(Bound2 a, Bound2 b) => a.AsVector256() == b.AsVector256();
    public static bool operator !=(Bound2 a, Bound2 b) => a.AsVector256() != b.AsVector256();
}
