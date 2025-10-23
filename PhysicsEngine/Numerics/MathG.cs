using System.Numerics;

namespace PhysicsEngine.Numerics;

public static partial class MathG
{
    public static T NaiveFMod<T>(T n, T d)
        where T : IFloatingPoint<T>
    {
        return n - T.Truncate(n / d) * d;
    }
}
