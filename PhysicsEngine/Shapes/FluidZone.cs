using System;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public struct FluidZone(BodyId id) : IShapeId, IZone2D, ITransform2D
{
    public static ShapeKind Kind => ShapeKind.FluidZone;

    public Bound2 Bounds;
    public double Density;
    
    public BodyId Id { get; set; } = id;

    public Double2 Position
    {
        readonly get => Bounds.Min;
        set => Bounds = Bounds.WithPosition(value);
    }

    public readonly double GetArea() => Bounds.GetArea();

    public readonly Bound2 GetBounds() => Bounds;

    public readonly void Apply<T>(ref T body, Bound2 intersection, Double2 gravity)
         where T : IShape2D, IRigidBody2D
    {
        // TODO: use more accurate intersection?
        double area = intersection.GetArea() / Math.PI;
        Double2 force = (Density * area) * -gravity;
        body.ApplyForce(force);
    }

    public readonly void Update(double deltaTime)
    {
    }
}