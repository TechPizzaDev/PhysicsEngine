using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace PhysicsEngine.Numerics;

public static class Double2Extensions
{
    public static StringBuilder Append(
        this StringBuilder builder,
        Double2 value,
        [StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format,
        IFormatProvider? formatProvider)
    {
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        return builder
            .Append('<')
            .Append(value.X.ToString(format, formatProvider))
            .Append(separator)
            .Append(' ')
            .Append(value.Y.ToString(format, formatProvider))
            .Append('>');
    }
}
