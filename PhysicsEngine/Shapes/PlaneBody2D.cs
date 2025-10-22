using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public struct PlaneBody2D(BodyId id) : IBodyId, ITransform2D, IRigidBody2D
{
    public Plane2D Data;

    public readonly BodyId Id => id;

    public Double2 Position { get => default; set { } }

    public Double2 Velocity => default;

    public double InverseMass => 1.0 / 1_000_000_000;

    public double RestitutionCoeff => 0;

    public void ApplyForce(Double2 force)
    {
    }

    public void ApplyImpulse(Double2 impulse, Double2 contactVector)
    {
    }
}
