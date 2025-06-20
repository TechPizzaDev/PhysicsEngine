using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace PhysicsEngine.Numerics;

public static class Bound2Extensions
{
    public static StringBuilder Append(
        this StringBuilder builder,
        Bound2 value,
        [StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format,
        IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return builder
            .Append("{Min:")
            .Append(value.Min, format, formatProvider)
            .Append(separator)
            .Append(" Max:")
            .Append(value.Max, format, formatProvider)
            .Append('}');
    }
}
