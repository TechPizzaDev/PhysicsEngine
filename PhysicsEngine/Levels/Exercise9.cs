using System;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class Exercise9(int cols, int rows, Random random) : Exercise8(cols, rows, random)
{
    public Exercise9(Random random) : this(4, 3, random)
    {
    }

    public override void SpawnPlanes(double w, double h)
    {
        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(0.1, 0.9).Normalize(), h) // Top
        });

        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(-0.1, 0.9).Normalize(), h) // Top
        });

        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(-0.1, -0.9).Normalize(), h) // Bottom
        });

        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(-0.9, -0.1).Normalize(), w) // Left
        });

        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(0.9, -0.1).Normalize(), w) // Right
        });
    }
}
