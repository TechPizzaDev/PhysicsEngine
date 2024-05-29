using MonoGame.Framework;
using MonoGame.Framework.Graphics;
using System.Numerics;

namespace PhysicsEngine
{
    public static class SpriteBatchExtensions
    {
        public static void DrawShadedString(
            this SpriteBatch spriteBatch,
            SpriteFont font,
            RuneEnumerator value,
            Vector2 position,
            Vector2 scale,
            Vector2 shadowScale,
            Color textColor,
            Color leftShadowColor,
            Color rightShadowColor)
        {
            spriteBatch.DrawString(font, value, position + new Vector2(shadowScale.X, -shadowScale.Y) * scale, rightShadowColor, 0, Vector2.Zero, scale, SpriteFlip.None, 0);

            spriteBatch.DrawString(font, value, position + new Vector2(-shadowScale.X, -shadowScale.Y) * scale, leftShadowColor, 0, Vector2.Zero, scale, SpriteFlip.None, 0);

            spriteBatch.DrawString(font, value, position + new Vector2(-shadowScale.X, shadowScale.Y) * scale, leftShadowColor, 0, Vector2.Zero, scale, SpriteFlip.None, 0);

            spriteBatch.DrawString(font, value, position + new Vector2(shadowScale.X, shadowScale.Y) * scale, rightShadowColor, 0, Vector2.Zero, scale, SpriteFlip.None, 0);

            spriteBatch.DrawString(font, value, position, textColor, 0, Vector2.Zero, scale, SpriteFlip.None, 0);
        }

        public static void DrawShadedString(
            this SpriteBatch spriteBatch,
            SpriteFont font,
            RuneEnumerator value,
            Vector2 position,
            Vector2 scale,
            Vector2 shadowScale,
            Color textColor,
            Color shadowColor)
        {
            DrawShadedString(
                spriteBatch,
                font,
                value,
                position,
                scale,
                shadowScale,
                textColor,
                shadowColor,
                shadowColor);
        }
    }
}
