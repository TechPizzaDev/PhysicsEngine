using System;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class ExerciseWorld : World
{
    public ExerciseWorld(Random random) : base(random)
    {
    }

    public ExerciseWorld() : this(new Random(1234))
    {
    }
}

public class Exercise1 : ExerciseWorld
{
    public Exercise1()
    {
        Add(new CircleBody()
        {
            Radius = 1,
            Density = 250
        }).CalculateMass();
    }
}
