using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public interface IShape
{
    Bound2 GetBounds();

    double GetArea();
}
