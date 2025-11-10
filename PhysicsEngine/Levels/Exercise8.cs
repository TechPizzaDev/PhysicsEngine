using System;
using MonoGame.Framework;
using PhysicsEngine.Drawing;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class Exercise8 : ExerciseWorld
{
    public Exercise8(int cols, int rows, Random random) : base(random)
    {
        uint seed = (uint) GetType().GetHashCode() & 0xf;
        for (uint s = 0; s < seed; s++)
            random.Next();

        Physics.LineTrail = true;

        Color[] colors = [Color.Green, Color.SteelBlue, Color.Magenta, Color.SandyBrown];

        int i = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                SpawnCircle(random, colors[x % colors.Length], x, y, i++);
            }
        }

        SpawnPlanes(20, 10);
    }

    public Exercise8(Random random) : this(4, 3, random)
    {
    }

    public virtual ref CircleBody SpawnCircle(Random random, Color color, int x, int y, int i)
    {
        ref CircleBody circle = ref Add(new CircleBody()
        {
            Color = color,
            Radius = 1 - x * 0.125,
            Density = 250,
            trail = new Trail(60),
            Position = new Double2(x * 4 - 6, y * -3 + 4)
        });
        circle.CalculateMass();
        double vx = random.NextDouble() - 0.5;
        double vy = random.NextDouble() - 0.5;
        circle.RigidBody.Velocity = new Double2(vx, vy) * 50;
        circle.RigidBody.RestitutionCoeff = 1f - (x / 10f);
        circle.CollisionMask = (CollisionMask) (1u << i);
        return ref circle;
    }

    public virtual void SpawnPlanes(double w, double h)
    {
        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(0, 1.0), h) // Top
        });

        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(0, -1.0), h) // Bottom
        });

        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(-1.0, 0), w) // Left
        });

        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(1.0, 0), w) // Right
        });
    }
}
