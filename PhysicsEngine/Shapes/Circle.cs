using System.Numerics;
using MonoGame.Framework;

namespace PhysicsEngine.Shapes;

public struct Circle
{
    public Vector2 Position;
    public Vector2 Velocity;

    public float Radius;
    public float Mass;
    public Color Color;

    public Trail trail;
}
