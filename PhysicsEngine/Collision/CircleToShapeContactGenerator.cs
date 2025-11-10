using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Collision;

public readonly struct CircleToShapeContactGenerator<TShape> : IContactGenerator<CircleBody, TShape>
    where TShape : IShape2D
{
    public readonly void Generate<C>(ref CircleBody a, ref TShape shape, C contacts)
        where C : IConsumer<Contact2D>
    {
        if (!a.Circle.Intersect(shape.GetBounds(), out Double2 hit, out Distance distance))
        {
            return;
        }

        contacts.Accept(new Contact2D()
        {
            Normal = new Double2(0), // TODO: non-zero normal?
            Point = hit,
            Depth = distance,
        });
    }
}
