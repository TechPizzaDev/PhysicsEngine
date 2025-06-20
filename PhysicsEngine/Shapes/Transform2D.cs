using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public struct Transform2D : ITransform2D
{
    public Double2 Position;
    public double Rotation;

    Double2 ITransform2D.Position
    {
        readonly get => Position;
        set => Position = value;
    }
}
