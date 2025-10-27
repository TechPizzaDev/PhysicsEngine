using PhysicsEngine.Collision;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public readonly record struct ShapeLocation(ShapeKind Kind, BodyId Id, Contact2D Contact);
