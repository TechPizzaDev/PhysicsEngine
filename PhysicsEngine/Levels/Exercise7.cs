using System;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class Exercise7 : Exercise6
{
    public Exercise7(Random random) : base(3, random)
    {
    }

    public override ref CircleBody CreateCircle(out CircleState state, int index, double torque)
    {
        ref CircleBody circle = ref base.CreateCircle(out state, index, torque);
        circle.Radius = 0.5;
        circle.RigidBody.SkipFrames = (byte) (index * 3);
        circle.CalculateMass();
        return ref circle;
    }
}
