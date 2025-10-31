using System;
using System.Numerics;

namespace PhysicsEngine.Levels;

public abstract class ExerciseWorld : World
{
    public ExerciseWorld(Random random) : base(random)
    {
    }

    public ExerciseWorld() : this(new Random(1234))
    {
    }

    public override (Vector2? Position, float? Scale) GetInitialCameraState()
    {
        return (new Vector2(0, 0), 20);
    }
}
