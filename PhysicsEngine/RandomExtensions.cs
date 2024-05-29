using System;
using System.Numerics;

namespace PhysicsEngine
{
    public static class RandomExtensions
    {
        public static float NextSingle(this Random random, float min, float max)
        {
            return (max - min) * random.NextSingle() + min;
        }

        public static Vector2 NextVector2(this Random random, Vector2 min, Vector2 max)
        {
            return new Vector2(
                random.NextSingle(min.X, max.X),
                random.NextSingle(min.Y, max.Y));
        }

        public static float NextAngle(this Random random)
        {
            return NextSingle(random, -MathF.PI, MathF.PI);
        }

        public static Vector2 NextUnitVector(this Random random)
        {
            float angle = NextAngle(random);
            (float sin, float cos) = MathF.SinCos(angle);
            return new Vector2(cos, sin);
        }
    }
}
