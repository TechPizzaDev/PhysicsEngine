using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public interface IZone2D : IShape2D
{
    void Apply<T>(ref T body, double area, Double2 gravity)
        where T : IRigidBody2D;
}
