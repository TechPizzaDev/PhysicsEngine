using System;
using System.Runtime.CompilerServices;
using MonoGame.Framework;

namespace PhysicsEngine.Shapes;

[InlineArray(3)]
public struct ColorPalette
{
    private Color _e0;

    public ColorPalette(ReadOnlySpan<Color> values, Color fallback)
    {
        Span<Color> self = this;
        Span<Color> head = self[..Math.Min(values.Length, self.Length)];
        values[..head.Length].CopyTo(head);
        self[head.Length..].Fill(fallback);
    }

    public ColorPalette(ReadOnlySpan<Color> values) : this(values, Color.White)
    {
    }

    public ColorPalette(Color fallback) : this([], fallback)
    {
    }
}
