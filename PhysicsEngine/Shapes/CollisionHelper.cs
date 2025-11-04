using System.Runtime.CompilerServices;

namespace PhysicsEngine.Shapes;

public static class CollisionHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAnyMask(CollisionMask filter, CollisionMask a, CollisionMask b)
    {
        return (filter == 0) | ((filter & a & b) != 0);
    }
}
