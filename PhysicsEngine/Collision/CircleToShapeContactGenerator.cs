using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Collision;

public readonly struct CircleToShapeContactGenerator<TShape> : IContactGenerator<CircleBody, TShape>
    where TShape : IShape2D
{
    public bool Generate(ref CircleBody a, ref TShape shape, out Contact2D contact)
    {
        contact = default;

        // TODO: proper circle-to-bounds intersection
        var intersection = shape.GetBounds().Intersect(a.Circle.GetBounds());
        if (!intersection.HasArea())
        {
            return false;
        }

        contact = new Contact2D()
        {
            Normal = new Double2(0),
            Point = intersection.GetCenter(),
            Depth = intersection.Size.MaxAcross()
        };
        return true;
    }
}
