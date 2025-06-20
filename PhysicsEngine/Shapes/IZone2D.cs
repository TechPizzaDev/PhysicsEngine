using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public interface IZone2D : IShape2D
{
    void Apply<T>(ref T body, Bound2 intersection, Double2 gravity)
        where T : IShape2D, IRigidBody2D;
}
