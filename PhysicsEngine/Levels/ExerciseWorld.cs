using System;

namespace PhysicsEngine.Levels;

public abstract class ExerciseWorld : World
{
    public override string Name => GetType().Name;

    public ExerciseWorld(Random random) : base(random)
    {
    }

    public ExerciseWorld() : this(new Random(1234))
    {
    }
}
