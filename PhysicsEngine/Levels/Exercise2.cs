using System;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class Exercise2 : ExerciseWorld
{
    public Exercise2(Random random) : base(random)
    {
        Add(new CircleBody()
        {
            Radius = 1,
            Density = 250
        }).CalculateMass();
    }
}
