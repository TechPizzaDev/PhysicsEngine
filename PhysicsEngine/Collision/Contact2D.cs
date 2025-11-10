using System;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Collision;

public record struct Contact2D(Double2 Normal, Double2 Point, Distance Depth) : IEquatable<Contact2D>
{
}
