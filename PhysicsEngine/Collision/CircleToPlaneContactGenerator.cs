using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Collision;

public readonly struct CircleToPlaneContactGenerator : IContactGenerator<CircleBody, Plane2D>
{
    public bool Generate(ref CircleBody a, ref Plane2D plane, out Contact2D contact)
    {
        contact = default;

        Circle circle = new(a.Position, a.Radius);

        if (!plane.Intersect(circle, out Double2 hitA, out Double2 hitB, out double depth))
        {
            return false;
        }

        contact = new Contact2D()
        {
            Normal = plane.Normal,
            Point = (hitA + hitB) / 2,
            Depth = depth
        };
        return true;
    }
}
