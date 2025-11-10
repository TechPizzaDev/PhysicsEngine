using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public interface ITransform2D
{
    Double2 Position { get; set; }

    Double2 Center => Position;
}
