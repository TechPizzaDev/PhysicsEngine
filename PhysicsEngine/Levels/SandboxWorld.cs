using System;
using System.Numerics;
using MonoGame.Framework;
using MonoGame.Framework.Input;
using PhysicsEngine.Drawing;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class SandboxWorld : World
{
    public SandboxWorld(Random random) : base(random)
    {
        SetupPhysics();

        for (int i = 0; i < 500; i++)
        {
            SpawnCircle();
        }
    }

    private void SetupPhysics()
    {
        Add(new PlaneBody2D()
        {
            Data = new Plane2D(new Double2(0, -1), 0)
        });

        Add(new WindZone()
        {
            Bounds = new Bound2(new RectangleF(0, 0, 10000, 500)),
            Speed = 100f,
            Direction = new Double2(0, 1f),
            Drag = 1.05,
            Density = 1.22,
            TurbulenceAngle = Math.PI * 0.5,
            TurbulenceIntensity = 1f,
            TurbulenceScale = new Double2(0.001),
            TurbulenceDepth = 0.1,
        });

        Add(new WindZone()
        {
            Bounds = new Bound2(new RectangleF(500, 0, 10000, 5000)),
            Speed = 100f,
            Direction = new Double2(0, 1f),
            Drag = 1.05,
            Density = 1.22,
            TurbulenceAngle = Math.PI * 0.5,
            TurbulenceIntensity = 1f,
            TurbulenceScale = new Double2(0.001),
            TurbulenceDepth = 0.1,
            TurbulenceSeed = 1234,
        });

        Add(new FluidZone()
        {
            Bounds = new Bound2(new RectangleF(-10000, 0, 10000, 500)),
            Density = 997,
        });

        Add(new ExplosionBody2D()
        {
            Transform = new() { Position = new Double2(0, 3000) },
            Radius = 4000,
            Force = 100_000_000_000,
            Interval = 3,
        });
    }

    public ref CircleBody SpawnCircle()
    {
        return ref SpawnCircle(Random);
    }

    public ref CircleBody SpawnCircle(Random rng)
    {
        ref CircleBody circle = ref Add(new CircleBody()
        {
            Color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle(), 1f),
            Radius = rng.Next(20, 40),
            Density = 250f,
            trail = new Trail(512),
        });
        circle.Transform.Position = rng.NextVector2(new Vector2(-3000, 5000), new Vector2(3000, 0));

        circle.RigidBody.Velocity = new Double2(50, 50);
        circle.RigidBody.AngularVelocity = 8;
        circle.RigidBody.Torque = -150;
        circle.RigidBody.RestitutionCoeff = 0.5;

        circle.CalculateMass();

        return ref circle;
    }

    public override void Update(in InputState input, in FrameTime time, Matrix4x4 inverseSceneTransform)
    {
        if (input.NewKeyState.IsKeyDown(Keys.F6))
        {
            SpawnCircle();
        }

        base.Update(input, time, inverseSceneTransform);
    }
}
