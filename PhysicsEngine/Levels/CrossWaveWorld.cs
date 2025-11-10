using System;
using MonoGame.Framework;
using PhysicsEngine.Drawing;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class CrossWaveWorld : ExerciseWorld
{
    public CrossWaveWorld(Random random) : base(random)
    {
        Physics.LineTrail = true;

        for (int i = 0; i < 100; i++)
        {
            ref CircleBody circle = ref Add(new CircleBody()
            {
                Color = Color.White,
                Radius = 1,
                Density = 250,
                trail = new Trail(150),
                Position = new Double2(0, 5 + 10 * i)
            });
            circle.CalculateMass();
            circle.RigidBody.Velocity = new Double2(10 + i, 0);
            circle.RigidBody.RestitutionCoeff = 1f;
        }
        
        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(-0.5, -0.5).Normalize(), 0)
        });
        
        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(0.5, -0.5).Normalize(), 0)
        });
    }
}
