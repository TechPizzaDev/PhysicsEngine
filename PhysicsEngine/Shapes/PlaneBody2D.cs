using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public struct PlaneBody2D(BodyId id) : IShapeId, ITransform2D, IRigidBody2D
{
    public static ShapeKind Kind => ShapeKind.Plane;

    public Plane2D Data;

    public readonly BodyId Id => id;

    public readonly Double2 Position { get => default; set { } }

    public readonly Double2 Velocity => default;

    public readonly double InverseMass => 1.0 / 1_000_000_000;

    public readonly double RestitutionCoeff => 0;

    public readonly void ApplyForce(Double2 force)
    {
    }

    public readonly void ApplyImpulse(Double2 impulse, Double2 contactVector)
    {
    }
}
