using MonoGame.Framework;
using MonoGame.Framework.Graphics;
using PhysicsEngine.Drawing;

namespace PhysicsEngine;

public struct DrawState
{
    public AssetRegistry Assets;
    public SpriteBatch SpriteBatch;
    public float Scale;
    public float RenderScale;
    public FrameTime Time;
    public RenderPass RenderPass;
    public Viewport Viewport;

    public readonly float FinalScale => Scale / RenderScale;
}
