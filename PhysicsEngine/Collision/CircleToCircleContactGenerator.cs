using PhysicsEngine.Shapes;

namespace PhysicsEngine.Collision;

readonly struct CircleToCircleContactGenerator : IContactGenerator<CircleBody, CircleBody>
{
    public bool Generate(ref CircleBody a, ref CircleBody b, out Contact2D contact)
    {
        contact = default;

        Circle cA = new(a.Position, a.Radius);
        Circle cB = new(b.Position, b.Radius);

        Double2 v = b.Velocity - a.Velocity;
        Double2 normal = cB.Origin - cA.Origin;

        if (Double2.Dot(v, normal) > 0)
        {
            // circles are moving apart
            return false;
        }

        if ((cA.Intersect(cB, out Double2 hitA, out Double2 hitB, out double distance) & IntersectionResult.Cuts) == 0)
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
