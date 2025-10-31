using System.Numerics;
using MonoGame.Framework;

namespace PhysicsEngine;

public struct UpdateState
{
    public InputState Input;
    public FrameTime Time;
    public float Scale;
    public Matrix4x4 InverseSceneTransform;
}
