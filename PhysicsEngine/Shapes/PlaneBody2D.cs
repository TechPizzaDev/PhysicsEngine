using System.Numerics;
using MonoGame.Framework;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public struct PlaneBody2D : IShapeId, ITransform2D, IRigidBody2D, IColor
{
    public static ShapeKind Kind => ShapeKind.Plane;

    public Plane2D Data;
    public CollisionMask CollisionMask;

    public BodyId Id { get; set; }
    public Color Color { get; set; }

    public PlaneBody2D()
    {
        Color = new Color(Color.Purple, 200);
        CollisionMask = CollisionMask.All;
    }

    public readonly Double2 Position { get => default; set { } }

    public readonly Double2 Velocity => default;

    public readonly double InverseMass => 0;

    public readonly double RestitutionCoeff => 0;
    
    public readonly void ApplyForce(Double2 force)
    {
    }

    public readonly void ApplyImpulse(Double2 impulse, Double2 contactVector)
    {
    }
}
