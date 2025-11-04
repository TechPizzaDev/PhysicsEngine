using System;
using MonoGame.Framework;
using PhysicsEngine.Drawing;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public struct CircleBody : IShapeId, IRigidBody2D, IShape2D, ITransform2D
{
    public static ShapeKind Kind => ShapeKind.Circle;

    public Transform2D Transform;
    public RigidBody2D RigidBody;
    public CollisionMask CollisionMask;

    public double Radius;
    public double Density;

    public Color Color;

    public Trail? trail;

    public BodyId Id { get; set; }

    public CircleBody()
    {
        CollisionMask = CollisionMask.All;
        Radius = 1;
        Density = 0;
        Color = Color.White;
    }

    public readonly Double2 Velocity => RigidBody.Velocity;

    public readonly double InverseMass => RigidBody.InverseMass;

    public readonly double RestitutionCoeff => RigidBody.RestitutionCoeff;

    public Double2 Position
    {
        readonly get => Transform.Position;
        set => Transform.Position = value;
    }

    public readonly Circle Circle => new(Transform.Position, Radius);

    public readonly Bound2 GetBounds() => Circle.GetBounds();

    public readonly double GetArea() => Circle.GetArea();

    public void ApplyForce(Double2 force) => RigidBody.ApplyForce(force);

    public void ApplyImpulse(Double2 impulse, Double2 contactVector) => RigidBody.ApplyImpulse(impulse, contactVector);

    public void CalculateMass()
    {
        double rSq = Radius * Radius;
        double mass = Math.PI * rSq * Density;
        double inertia = mass * rSq / 2;

        RigidBody.InverseMass = mass != 0 ? 1.0 / mass : 0.0;
        RigidBody.InverseInertia = inertia != 0 ? 1.0 / inertia : 0.0;
    }
}
