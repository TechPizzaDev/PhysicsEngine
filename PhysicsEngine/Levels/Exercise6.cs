using System;

namespace PhysicsEngine.Levels;

public class Exercise6 : Exercise5
{
    public Exercise6(int count, Random random) : base(count, random)
    {
        _labelAngle = false;
    }

    public Exercise6(Random random) : this(4, random)
    {
    }
}
