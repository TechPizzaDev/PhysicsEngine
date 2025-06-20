using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public readonly struct PlaneBody2D : ITransform2D, IRigidBody2D
{
    public readonly Plane2D Data;

    public Double2 Position { get => default; set { } }

    public Double2 Velocity => default;

    public double InverseMass => 1.0 / 1_000_000_000;

    public double RestitutionCoeff => 0;

    public void ApplyImpulse(Double2 impulse, Double2 contactVector)
    {
    }
}
