using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Collision;

struct CircleToCircleContactGenerator(
    bool requireSharedTrajectory,
    IntersectionResult intersectMask,
    CollisionMask collisionFilter
) : IContactGenerator<CircleBody, CircleBody>
{
    public bool RequireSharedTrajectory = requireSharedTrajectory;
    public IntersectionResult IntersectMask = intersectMask;
    public CollisionMask CollisionFilter = collisionFilter;

    public readonly void Generate<C>(ref CircleBody a, ref CircleBody b, C contacts)
        where C : IConsumer<Contact2D>
    {
        if (!CollisionHelper.HasAnyMask(CollisionFilter, a.CollisionMask, b.CollisionMask))
        {
            return;
        }

        Circle cA = a.Circle;
        Circle cB = b.Circle;

        if ((cA.Intersect(cB, out Double2 hit, out Distance distance, out _) & IntersectMask) == 0)
        {
            return;
        }

        double d = distance.GetEuclidean();
        contacts.Accept(new Contact2D()
        {
            Normal = (cB.Origin - cA.Origin) / d,
            Point = hit,
            Depth = Distance.Euclidean(cA.Radius + cB.Radius - d),
        });
    }
}
