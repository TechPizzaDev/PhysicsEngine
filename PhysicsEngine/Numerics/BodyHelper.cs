using System;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Numerics;

public static class BodyHelper
{
    public static int IndexOf<T>(BodyId id, ReadOnlySpan<T> span)
        where T : IShapeId
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Id == id)
            {
                return i;
            }
        }
        return -1;
    }

    public static int IndexOf<T>(BodyId id, Span<T> span)
        where T : IShapeId
    {
        return IndexOf(id, (ReadOnlySpan<T>) span);
    }

}
