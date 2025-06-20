using System;
using System.Runtime.Intrinsics;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public readonly struct LineSegment
{
    public readonly Double2 Start;
    public readonly Double2 End;

    public LineSegment(Double2 start, Double2 end)
    {
        Start = start;
        End = end;
    }

    public bool Intersect(LineSegment line, out Double2 intersection)
    {
        Double2 start = Start;
        Double2 d1 = End - start;
        Double2 d2 = line.End - line.Start;
        Double2 d3 = start - line.Start;

        double det = Double2.Cross(d1, d2);
        double t1 = Double2.Cross(d2, d3) / det;
        double t2 = Double2.Cross(d1, d3) / det;
        intersection = start + t1 * d1;

        Vector128<double> t = Vector128.Create(t1, t2);
        Vector128<double> neg = Vector128.GreaterThanOrEqual(t, Vector128<double>.Zero);
        Vector128<double> pos = Vector128.LessThanOrEqual(t, Vector128<double>.One);
        return Vector128.EqualsAll((neg & pos).AsInt64(), Vector128<long>.AllBitsSet);
    }

    public bool Intersect(Plane2D plane, out Double2 intersection)
    {
        Double2 start = Start;
        Double2 end = End;

        double l0 = Double2.Dot(start, plane.Normal) - plane.D;
        double l1 = Double2.Dot(end, plane.Normal) - plane.D;
        double t = l0 / (l0 - l1);
        intersection = start + (end - start) * t;

        return l0 * l1 <= 0;
    }

    private (double c, double b, double len, Double2 norm) CutCircle(Circle circle)
    {
        Double2 start = Start;
        Double2 delta = End - start;
        Double2 m = start - circle.Origin;
        double c = m.LengthSquared() - circle.Radius * circle.Radius;

        double len = delta.Length();
        Double2 norm = delta / len;

        double b = Double2.Dot(m, norm);
        return (c, b, len, norm);
    }

    public int Intersect(Circle circle, out Double2 hitA, out Double2 hitB)
    {
        (double c, double b, double len, Double2 norm) = CutCircle(circle);

        double disc = Math.Sqrt(b * b - c);
        double tmin = Math.Max(-b - disc, 0);
        double tmax = Math.Min(disc - b, len);

        hitA = Start + tmin * norm;
        hitB = Start + tmax * norm;

        if (c > 0 && b > 0)
            return 0;

        if (tmin <= len)
        {
            return tmax == tmin ? 1 : 2;
        }
        return 0;
    }

    public bool Intersect(Circle circle)
    {
        (double c, double b, double len, _) = CutCircle(circle);

        if (c > 0 && b > 0)
            return false;

        double disc = Math.Sqrt(b * b - c);
        double tmin = Math.Max(-b - disc, 0);

        return tmin <= len;
    }
}
