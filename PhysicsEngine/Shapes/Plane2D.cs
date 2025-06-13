using System;

namespace PhysicsEngine.Shapes;

public readonly struct Plane2D
{
    public readonly Double2 Normal;
    public readonly double D;

    public Plane2D(Double2 normal, double d)
    {
        Normal = normal;
        D = d;
    }

    public double DistanceTo(Double2 point)
    {
        return Double2.Dot(point, Normal) - D;
    }

    public bool Intersect(Circle circle)
    {
        return DistanceTo(circle.Origin) <= circle.Radius;
    }

    public bool Intersect(Circle circle, out Double2 hitA, out Double2 hitB, out double depth)
    {
        double dist = DistanceTo(circle.Origin);

        double r = circle.Radius;
        double overlap = Math.Sqrt(Math.Abs(r * r - dist * dist));
        depth = overlap;
        
        Double2 ortho = Normal.RotateCW() * overlap;
        Double2 near = circle.Origin - Normal * dist;
        hitA = near - ortho;
        hitB = near + ortho;

        return Math.Abs(dist) <= r;
    }
}
