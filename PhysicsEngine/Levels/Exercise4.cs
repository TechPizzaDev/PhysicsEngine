using System;
using System.Numerics;
using MonoGame.Framework;
using PhysicsEngine.Drawing;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class Exercise4 : ExerciseWorld
{
    private Trail _analyticalPath;

    public Exercise4(Random random) : base(random)
    {
        Physics.LineTrail = true;
        Physics.VelocityMode = VelocityMethod.Naive;

        _analyticalPath = new Trail(300)
        {
            FadeFactor = 0f,
        };

        Color[] colors =
        [
            Color.Red,
            Color.Green,
            Color.Blue,
        ];

        Double2 p0 = new(-10, 0);
        Double2 v0 = new(10, 10);

        for (int i = 0; i < colors.Length; i++)
        {
            var mask = (CollisionMask) (1u << i);

            ref CircleBody circle = ref Add(new CircleBody()
            {
                Color = colors[i],
                Radius = 1,
                Density = 250,
                trail = new Trail(150),
                Position = p0,
                CollisionMask = mask,
            });
            circle.CalculateMass();
            circle.RigidBody.Velocity = v0;
            circle.RigidBody.RestitutionCoeff = 1.0;
            circle.RigidBody.SkipFrames = (byte) (i * 3);

            Add(new PlaneBody2D()
            {
                Data = new Plane2D(new Double2(0, -1), (i + 1) * 2),
                CollisionMask = mask,
                Color = new Color(colors[i], 127),
            });
        }

        Double2 g = Physics.Gravity;
        for (int i = 0; i < _analyticalPath.Capacity; i++)
        {
            double t = i / 100.0;
            _analyticalPath.Push((Vector2) Path(p0, v0, g, t));
        }
    }

    protected override void DrawWorld(in DrawState state)
    {
        _analyticalPath.DrawLines(state.SpriteBatch, Color.White, 1f / state.FinalScale);

        base.DrawWorld(state);
    }

    private static Double2 Path(Double2 p0, Double2 v0, Double2 g, double t)
    {
        return p0 + v0 * t + (g * t * t) / 2;
    }
}
