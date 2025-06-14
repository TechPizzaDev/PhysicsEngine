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

    public bool Intersect(Circle circle, out double depthSquared)
    {
        double rA = Radius;
        double rB = circle.Radius;

        double rSum = rA + rB;
        double rSq = rSum * rSum;

        Double2 delta = circle.Origin - Origin;
        double distSq = delta.LengthSquared();
        depthSquared = distSq;

        return distSq <= rSq;
    }

    public IntersectionResult Intersect(Circle circle, out Double2 hitA, out Double2 hitB, out double distance)
    {
        Double2 delta = circle.Origin - Origin;
        double dist = Math.Sqrt(delta.LengthSquared());
        delta /= dist;

        double rA = Radius;
        double rB = circle.Radius;
        distance = dist;

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
