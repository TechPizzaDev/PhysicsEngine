using PhysicsEngine.Collision;
using PhysicsEngine.Numerics;

namespace PhysicsEngine;

public readonly record struct ShapeLocation(ShapeKind Kind, BodyId Id, Contact2D Contact);
