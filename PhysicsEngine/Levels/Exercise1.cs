using System;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class Exercise1 : ExerciseWorld
{
    public Exercise1(Random random) : base(random)
    {
        Physics.Gravity = new Double2(0);

        _labelRadius = true;
        _labelMass = true;
        _labelInertia = true;

        Add(new CircleBody()
        {
            Radius = 1,
            Density = 250
        }).CalculateMass();
    }
}
