using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Collision;

public readonly struct CircleToPlaneContactGenerator : IContactGenerator<CircleBody, PlaneBody2D>
{
    public bool Generate(ref CircleBody a, ref PlaneBody2D plane, out Contact2D contact)
    {
        contact = default;

        if (!plane.Data.Intersect(a.Circle, out Double2 hitA, out Double2 hitB, out double depth))
        {
            return false;
        }

        contact = new Contact2D()
        {
            Normal = plane.Data.Normal,
            Point = (hitA + hitB) / 2,
            Depth = depth
        };
        return true;
    }
}
