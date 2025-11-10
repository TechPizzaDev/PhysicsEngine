namespace PhysicsEngine.Numerics;

public readonly record struct Distance(double Value)
{
    public bool IsSquared => Value >= 0;
    public bool IsEuclidean => Value < 0;

    public double GetSquared() => IsEuclidean ? (Value * Value) : Value;

    public double GetEuclidean() => IsEuclidean ? -Value : Math.Sqrt(Value);

    public static Distance Squared(double value) => new(Math.Abs(value));

    public static Distance Euclidean(double value) => new(-Math.Abs(value));
}
