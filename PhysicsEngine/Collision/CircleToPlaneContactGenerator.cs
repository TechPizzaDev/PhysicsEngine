using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Collision;

public struct CircleToPlaneContactGenerator(
    CollisionMask collisionFilter
) : IContactGenerator<CircleBody, PlaneBody2D>
{
    public CollisionMask CollisionFilter = collisionFilter;

    public readonly void Generate<C>(ref CircleBody a, ref PlaneBody2D plane, C contacts)
        where C : IConsumer<Contact2D>
    {
        if (!CollisionHelper.HasAnyMask(CollisionFilter, a.CollisionMask, plane.CollisionMask))
        {
            return;
        }

        if (!plane.Data.Intersect(a.Circle, out Double2 hit, out Distance depth))
        {
            return;
        }

        contacts.Accept(new Contact2D()
        {
            Normal = plane.Data.Normal,
            Point = hit,
            Depth = depth
        });
    }
}
