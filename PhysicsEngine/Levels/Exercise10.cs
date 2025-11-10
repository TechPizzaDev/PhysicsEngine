using System;
using MonoGame.Framework;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class Exercise10(int cols, int rows, Random random) : Exercise9(cols, rows, random)
{
    public Exercise10(Random random) : this(4, 3, random)
    {
    }

    public override ref CircleBody SpawnCircle(Random random, Color color, int x, int y, int i)
    {
        ref CircleBody circle = ref base.SpawnCircle(random, color, x, y, i);
        circle.CollisionMask = CollisionMask.All;
        return ref circle;
    }
}
