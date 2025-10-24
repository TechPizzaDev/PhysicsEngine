using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public interface IShape2D
{
    Bound2 GetBounds();

    double GetArea();
}
