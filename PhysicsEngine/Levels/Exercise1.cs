using System;
using MonoGame.Framework;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class Exercise1 : ExerciseWorld
{
    public Exercise1(Random random) : base(random)
    {
        Physics.Gravity = new Double2(0);

        Add(new CircleBody()
        {
            Color = Color.White,
            Radius = 1,
            Density = 250
        }).CalculateMass();
    }
}
