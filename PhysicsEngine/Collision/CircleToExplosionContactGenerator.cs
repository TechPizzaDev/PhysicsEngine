using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Collision;

struct CircleToExplosionContactGenerator(
    IntersectionResult mask) : IContactGenerator<CircleBody, ExplosionBody2D>
{
    public IntersectionResult Mask = mask;

    public readonly void Generate<C>(ref CircleBody a, ref ExplosionBody2D b, C contacts)
        where C : IConsumer<Contact2D>
    {
        Circle cA = a.Circle;
        Circle cB = b.Circle;

        if ((cA.Intersect(cB, out Double2 hit, out Distance distance, out _) & Mask) == 0)
        {
            return;
        }

        double d = distance.GetEuclidean();
        contacts.Accept(new Contact2D()
        {
            Normal = (cB.Origin - cA.Origin) / d,
            Point = hit,
            Depth = Distance.Euclidean(cA.Radius + cB.Radius - d)
        });
    }
}
