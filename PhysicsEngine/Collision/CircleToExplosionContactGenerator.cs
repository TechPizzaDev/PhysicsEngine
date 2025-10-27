using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Collision;

struct CircleToExplosionContactGenerator(
    IntersectionResult mask) : IContactGenerator<CircleBody, ExplosionBody2D>
{
    public IntersectionResult Mask = mask;

    public readonly bool Generate(ref CircleBody a, ref ExplosionBody2D b, out Contact2D contact)
    {
        contact = default;

        Circle cA = a.Circle;
        Circle cB = b.Circle;

        if ((cA.Intersect(cB, out Double2 hitA, out Double2 hitB, out double distance) & Mask) == 0)
        {
            return false;
        }
        
        Double2 normal = cB.Origin - cA.Origin;

        contact = new Contact2D()
        {
            Normal = normal / distance,
            Point = (hitA + hitB) / 2,
            Depth = cA.Radius + cB.Radius - distance
        };
        return true;
    }
}
