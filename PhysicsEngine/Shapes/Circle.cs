using System;
using System.Runtime.CompilerServices;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public readonly struct Circle : IShape2D
{
    public readonly Double2 Origin;
    public readonly double Radius;

    public Circle(Double2 origin, double radius)
    {
        Origin = origin;
        Radius = radius;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bound2 GetBounds()
    {
        Double2 size = new(Radius);
        return new Bound2(Origin - size, Origin + size);
    }

    public double GetArea() => Radius * Radius * Math.PI;

    public IntersectionResult Intersect(Circle circle, out Double2 hit, out Distance distance, out Distance edge)
    {
        double rA = Radius;
        double rB = circle.Radius;

        Double2 delta = circle.Origin - Origin;
        double dSq = delta.LengthSquared();
        double eSq = (rA * rA - rB * rB + dSq) / (dSq + dSq);

        hit = Origin + eSq * delta;
        distance = Distance.Squared(dSq);
        edge = Distance.Squared(eSq);

        double r1 = rA + rB;
        double r2 = rA - rB;
        bool overlaps = dSq <= (r1 * r1);
        bool cuts = dSq >= (r2 * r2);
        return IntersectionHelper.MakeResult(overlaps, cuts);
    }

    public IntersectionResult Intersect(
        Circle circle, out Double2 hitA, out Double2 hitB, out Distance distance, out Distance edge)
    {
        IntersectionResult result = Intersect(circle, out Double2 hit, out distance, out edge);
        if (result == IntersectionResult.None)
        {
            hitA = hit;
            hitB = hit;
            return result;
        }

        Double2 delta = circle.Origin - Origin;
        double height = Math.Sqrt(Math.Abs(Radius * Radius - edge.GetSquared()));

        Double2 ortho = (delta * height).RotateCW();
        hitA = hit - ortho;
        hitB = hit + ortho;
        return result;
    }

    public bool Intersect(Bound2 bound, out Double2 hit, out Distance distance)
    {
        Double2 closest = bound.ClosestTo(Origin);
        hit = closest;

        double distSq = (closest - Origin).LengthSquared();
        distance = Distance.Squared(distSq);

        return distSq <= Radius * Radius;
    }
}
