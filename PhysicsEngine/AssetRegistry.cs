using MonoGame.Framework;
using MonoGame.Framework.Content;
using MonoGame.Framework.Graphics;

namespace PhysicsEngine
{
    public class AssetRegistry
    {
        public AssetRegistry(ContentManager content)
        {
            Font_Consolas = content.Load<SpriteFont>("consolas");
        }

        public SpriteFont Font_Consolas { get; }
    }
}