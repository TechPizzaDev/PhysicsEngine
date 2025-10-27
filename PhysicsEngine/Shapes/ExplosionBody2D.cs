using System;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public struct ExplosionBody2D(BodyId id) : IShapeId, IShape2D, ITransform2D
{
    public static ShapeKind Kind => ShapeKind.Explosion;

    public Transform2D Transform;
    public double Radius;
    public double Force;
    public double Time;
    public double Interval;
    public bool ShouldApply;

    public readonly BodyId Id => id;

    public Double2 Position
    {
        readonly get => Transform.Position;
        set => Transform.Position = value;
    }

    public readonly Circle Circle => new(Transform.Position, Radius);

    public readonly Bound2 GetBounds() => Circle.GetBounds();

    public readonly double GetArea() => Circle.GetArea();

    public void Update(double deltaTime)
    {
        if (ShouldApply)
        {
            ShouldApply = false;
            Time = 0;
        }

        Time += deltaTime;
        if (Time >= Interval)
        {
            ShouldApply = true;
        }
    }

    public readonly void Apply<T>(ref T body)
        where T : IRigidBody2D, ITransform2D
    {
        Double2 normal = body.Position - Position;
        double distanceSq = normal.LengthSquared();
        if (distanceSq > Radius * Radius)
        {
            return;
        }

        double distance = Math.Sqrt(distanceSq);
        Double2 direction = normal / distance;
        double attenuation = 1.0 - distance / Radius;
        double strength = Force * (attenuation * attenuation);
        body.ApplyForce(direction * strength);
    }
}
