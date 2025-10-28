using System;

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
