using System;

namespace PhysicsEngine.Numerics;

public readonly record struct BodyId(uint Value) : IEquatable<BodyId>
{
    public override string ToString() => Value.ToString("X");
}
