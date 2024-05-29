using System;
using System.Numerics;
using MonoGame.Framework;
using MonoGame.Framework.Graphics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine;

public class World
{
    public Storage<Circle> circles = new(1024);

    public Random rng;

    public Vector2 Gravity = new(9.82f, 0);

    public float TotalTime;
    public float TimeScale = 1f;

    public World(Random random)
    {
        rng = random;

        for (int i = 0; i < 5; i++)
        {
            SpawnCircle();
        }
    }

    public ref Circle SpawnCircle()
    {
        ref Circle circle = ref circles.Add();
        circle = new Circle
        {
            Position = rng.NextVector2(new Vector2(-1500, -1000), new Vector2(1500, 0)),
            Color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle(), 1f),
            Radius = rng.Next(40, 60),
            trail = new Trail(16),
        };
        circle.Mass = circle.Radius * 0.1f;
        return ref circle;
    }

    public void Update(in InputState input, in FrameTime time)
    {
        float deltaTime = time.ElapsedTotalSeconds * TimeScale;
        FixedUpdate(TotalTime, deltaTime);
        TotalTime += deltaTime;
    }

    public void FixedUpdate(float totalTime, float deltaTime)
    {
        foreach (ref Circle circle in circles.AsSpan())
        {
            circle.Velocity += Gravity * circle.Mass;
            circle.Position += circle.Velocity * deltaTime;

            circle.trail.Update(circle.Position);
        }
    }

    public void Draw(RenderPass renderPass, AssetRegistry assets, SpriteBatch spriteBatch)
    {
        if (renderPass == RenderPass.Scene)
        {
            foreach (ref Circle circle in circles.AsSpan())
            {
                spriteBatch.DrawCircle(circle.Position, circle.Radius, Math.Max(8, (int) (circle.Radius / 2f)), circle.Color, 8f);
            }

            foreach (ref Circle circle in circles.AsSpan())
            {
                circle.trail.Draw(spriteBatch, circle.Color, circle.Radius / 4f);
            }
        }
    }
}
