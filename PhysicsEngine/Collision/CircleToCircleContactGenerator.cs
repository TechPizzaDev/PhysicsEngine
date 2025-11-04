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

    public readonly bool Generate(ref CircleBody a, ref CircleBody b, out Contact2D contact)
    {
        contact = default;
        if (!CollisionHelper.HasAnyMask(CollisionFilter, a.CollisionMask, b.CollisionMask))
        {
            return false;
        }

        Circle cA = a.Circle;
        Circle cB = b.Circle;

        Double2 normal = cB.Origin - cA.Origin;
        if (RequireSharedTrajectory)
        {
            Double2 v = b.Velocity - a.Velocity;
            if (Double2.Dot(v, normal) > 0)
            {
                // circles are moving apart
                return false;
            }
        }

        if ((cA.Intersect(cB, out Double2 hitA, out Double2 hitB, out double distance) & IntersectMask) == 0)
        {
            return false;
        }

        contact = new Contact2D()
        {
            Normal = normal / distance,
            Point = (hitA + hitB) / 2,
            Depth = cA.Radius + cB.Radius - distance
        };
        return true;
    }
}
