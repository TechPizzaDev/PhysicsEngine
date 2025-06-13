using System;

namespace PhysicsEngine.Shapes;

public readonly struct Circle
{
    public readonly Double2 Origin;
    public readonly double Radius;

    public Circle(Double2 origin, double radius)
    {
        Origin = origin;
        Radius = radius;
    }

    public bool Intersect(Circle circle)
    {
        double rSum = circle.Radius + Radius;
        double rSq = rSum * rSum;

        Double2 dist = circle.Origin - Origin;
        double dSq = dist.LengthSquared();

        return dSq <= rSq;
    }

    public IntersectionResult Intersect(Circle circle, out Double2 hitA, out Double2 hitB, out double depth)
    {
        Double2 delta = circle.Origin - Origin;
        double distSq = delta.LengthSquared();
        double dist = Math.Sqrt(distSq);
        delta /= dist;
        depth = dist;

        double rA = Radius;
        double rB = circle.Radius;

        bool overlaps = dist <= rA + rB;
        bool cuts = dist >= Math.Abs(rA - rB);
        IntersectionResult result = IntersectionHelper.MakeResult(overlaps, cuts);

        double a = (rA * rA - rB * rB + dist * dist) / (2 * dist);
        double height = Math.Sqrt(Math.Abs(rA * rA - a * a));

        Double2 p = Origin + a * delta;
        hitA = p + height * delta.RotateCCW();
        hitB = p + height * delta.RotateCW();

        return result;
    }
}
