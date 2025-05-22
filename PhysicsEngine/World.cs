using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using ImGuiNET;
using MonoGame.Framework;
using MonoGame.Framework.Graphics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine;

public class World
{
    public Storage<Circle> circles = new(1024);

    public Random rng;

    public Double2 Gravity = new(0, 9.82);

    public double TotalTime;
    public double TimeScale = 1f;

    private bool _drawTrails = true;

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
            Color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle(), 1f),
            Radius = rng.Next(1, 4),
            Density = 250f,
            trail = new Trail(512),
        };
        circle.Transform.Position = rng.NextVector2(new Vector2(-1500, -1000), new Vector2(1500, 0));

        circle.RigidBody.Velocity = new Double2(50, -50);

        circle.CalculateMass();

        return ref circle;
    }

    public void Update(in InputState input, in FrameTime time)
    {
        double deltaTime = 1 / 60.0 * TimeScale;
        FixedUpdate(deltaTime);
        TotalTime += deltaTime;

        ImGui.Begin("World");

        ImGui.Checkbox("Draw Trails", ref _drawTrails);

        ImGui.End();
    }

    public void FixedUpdate(double deltaTime)
    {
        foreach (ref Circle circle in circles.AsSpan())
        {
            circle.RigidBody.IntegrateForces(Gravity, deltaTime);
        }

        foreach (ref Circle circle in circles.AsSpan())
        {
            circle.RigidBody.IntegrateVelocity(ref circle.Transform, Gravity, deltaTime);
        }

        if (_drawTrails)
        {
            foreach (ref Circle circle in circles.AsSpan())
            {
                circle.trail.Update((Vector2) circle.Transform.Position);
            }
        }
    }

    public void Draw(RenderPass renderPass, AssetRegistry assets, SpriteBatch spriteBatch)
    {
        float scale = 16;

        if (renderPass == RenderPass.Scene)
        {
            DrawWorld(assets, spriteBatch, scale);
        }
    }

    private void DrawWorld(AssetRegistry assets, SpriteBatch spriteBatch, float scale)
    {
        foreach (ref Circle circle in circles.AsSpan())
        {
            spriteBatch.DrawCircle(
                (Vector2) circle.Transform.Position,
                scale * (float) circle.Radius,
                (int) (scale * Math.Max(8, (float) circle.Radius / 2f)),
                circle.Color,
                8f);
        }

        if (_drawTrails)
        {
            foreach (ref Circle circle in circles.AsSpan())
            {
                circle.trail.Draw(spriteBatch, circle.Color, scale * (float) circle.Radius / 2f);
            }
        }

        StringBuilder builder = new();
        foreach (ref Circle circle in circles.AsSpan())
        {
            builder.Clear();
            builder.AppendLine(NumberFormatInfo.InvariantInfo, $"P {circle.Transform.Position:0.0}");
            builder.AppendLine(NumberFormatInfo.InvariantInfo, $"V {circle.RigidBody.Velocity:0.0}");

            spriteBatch.DrawString(
                assets.Font_Consolas, builder, (Vector2) circle.Transform.Position + new Vector2((float) circle.Radius * scale + 4, -8),
                circle.Color, 0, new Vector2(), new Vector2(0.5f), SpriteFlip.None, 0);
        }
    }
}
