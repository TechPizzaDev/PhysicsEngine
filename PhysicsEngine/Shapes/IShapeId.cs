using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public interface IShapeId
{
    static abstract ShapeKind Kind { get; }

    BodyId Id { get; }
}
