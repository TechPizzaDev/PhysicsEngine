using System;
using PhysicsEngine.Collision;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public readonly struct ShapeLocation(ShapeKind kind, BodyId id, Contact2D contact) : IEquatable<ShapeLocation>
{
    public ShapeKind Kind => kind;
    public BodyId Id => id;
    public Contact2D Contact => contact;

    public bool Equals(ShapeLocation other) => Kind == other.Kind && Id == other.Id;

    public override bool Equals(object? obj) => obj is ShapeLocation other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Kind, Id);

    public static bool operator ==(ShapeLocation left, ShapeLocation right) => left.Equals(right);
    public static bool operator !=(ShapeLocation left, ShapeLocation right) => !left.Equals(right);
}
