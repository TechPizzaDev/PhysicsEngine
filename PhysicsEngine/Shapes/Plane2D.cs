using System;
using PhysicsEngine.Numerics;

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

    public bool Intersect(Circle circle, out Double2 hit, out Distance depth)
    {
        double d = DistanceTo(circle.Origin);
        double dSq = d * d;
        double rSq = circle.Radius * circle.Radius;

        depth = Distance.Squared(Math.Abs(rSq - dSq));
        hit = circle.Origin - Normal * d;
        return dSq <= rSq;
    }

    public bool Intersect(Circle circle, out Double2 hitA, out Double2 hitB, out Distance depth)
    {
        if (!Intersect(circle, out Double2 hit, out depth))
        {
            hitA = hit;
            hitB = hit;
            return false;
        }

        Double2 ortho = Normal.RotateCW() * depth.GetEuclidean();
        hitA = hit - ortho;
        hitB = hit + ortho;
        return true;
    }
}
