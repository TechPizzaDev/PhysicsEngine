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
        for (int i = 0; i < 5; i++)
        {
            SpawnCircle();
        }

        for (int i = 0; i < 5; i++)
        {
            ref CircleBody circle = ref SpawnCircle();
            circle.Transform.Position *= 0.25;
            circle.RigidBody.Velocity = new();
        }

        for (int i = 0; i < 500; i++)
        {
            SpawnCircle();
        }
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
