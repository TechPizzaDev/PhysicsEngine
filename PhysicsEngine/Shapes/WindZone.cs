using System.Numerics;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public struct WindZone(BodyId id) : IShapeId, IZone2D, ITransform2D
{
    public static ShapeKind Kind => ShapeKind.WindZone;

    public Bound2 Bounds;
    public Double2 Direction;
    public double Speed;
    public double Density;
    public double Drag;

    public double TurbulenceAngle;
    public double TurbulenceIntensity;
    public Double2 TurbulenceScale;
    public double TurbulenceDepth;
    public int TurbulenceSeed;
    public double Time;

    public readonly BodyId Id => id;

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
        double windForce = 0.5 * Density * (Speed * Speed) * Drag * area;
        Double2 dir = Direction;

        (double turbAngle, double turbStrength) = EvaluateTurbulence(body.GetBounds().GetCenter());
        if (turbStrength != 0)
        {
            dir = dir.Rotate(Double2.SinCos(turbAngle));
            windForce *= turbStrength;
        }

        Double2 force = dir * windForce;
        body.ApplyForce(force);
    }

    public readonly (double Angle, double Strength) EvaluateTurbulence(Double2 position)
    {
        if (TurbulenceIntensity == 0)
            return (0, 0);

        Vector2 p = (Vector2) (position * TurbulenceScale);
        
        double noise = MathG.Simplex(p.X, p.Y, (float) Time, TurbulenceSeed);
        return (noise * TurbulenceAngle, (noise + 2) / 3f * TurbulenceIntensity);
    }

    public void Update(double deltaTime)
    {
        Time += deltaTime * TurbulenceDepth;
    }
}
