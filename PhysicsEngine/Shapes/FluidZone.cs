using System;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public struct FluidZone : IZone2D
{
    public Bound2 Bounds;
    public double Density;

    public Double2 Position
    {
        readonly get => Bounds.Min;
        set => Bounds = Bounds.WithPosition(value);
    }

    public readonly double GetArea() => Bounds.GetArea();

    public readonly Bound2 GetBounds() => Bounds;

    public readonly void Apply<T>(ref T body, double area, Double2 gravity)
         where T : IRigidBody2D
    {
        Double2 force = (Density * area / Math.PI) * -gravity;
        body.ApplyForce(force);
    }
}