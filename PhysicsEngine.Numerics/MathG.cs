using System.Numerics;

namespace PhysicsEngine.Numerics;

public static partial class MathG
{
    public static T NaiveFMod<T>(T n, T d)
        where T : IFloatingPoint<T>
    {
        return n - T.Truncate(n / d) * d;
    }

    public static T InverseLerp<T>(T a, T b, T value)
        where T : ISubtractionOperators<T, T, T>, IDivisionOperators<T, T, T>
    {
        return (value - a) / (b - a);
    }
}
