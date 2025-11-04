using System;
using MonoGame.Framework;
using PhysicsEngine.Drawing;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class Exercise2 : ExerciseWorld
{
    public Exercise2(Random random) : base(random)
    {
        Physics.LineTrail = true;

        ref CircleBody circle = ref Add(new CircleBody()
        {
            Color = Color.White,
            Radius = 1,
            Density = 250,
            trail = new Trail(150),
            Position = new Double2(-10, 0)
        });
        circle.CalculateMass();
        circle.RigidBody.Velocity = new Double2(10, 10);
        circle.RigidBody.RestitutionCoeff = 1f;

        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(0, -1), 1)
        });
    }
}
