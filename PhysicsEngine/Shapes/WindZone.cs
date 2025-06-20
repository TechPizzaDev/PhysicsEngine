using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public struct WindZone : IZone2D, ITransform2D
{
    public Bound2 Bounds;
    public Double2 Direction;
    public double Speed;
    public double Density;
    public double Drag;

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
        double area = body.GetArea();
        double windForce = 0.5 * Density * Speed * Speed * Drag * area;
        Double2 force = Direction * windForce;
        body.ApplyForce(force);
    }
}
