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
    public Storage<CircleBody> circles = new(1024);

    public Random rng;

    private StringBuilder _strBuilder = new();

    public Double2 Gravity = new(0, 9.82);

    public double TotalTime;
    public double TimeScale = 1f;

    private bool _enableVelocity = true;
    private bool _enableAngular = true;

    private bool _labelPosition;
    private bool _labelAngle;
    private bool _labelRadius;
    private bool _labelDensity;
    private bool _labelVelocity;
    private bool _labelAngular;
    private bool _labelMass;
    private bool _labelInertia;
    private bool _labelTorque;

    private bool _lineTrail = true;
    private bool _lineVelocity = false;
    private bool _lineAngle = true;
    private bool _lineForward = true;

    public World(Random random)
    {
        rng = random;

        for (int i = 0; i < 5; i++)
        {
            SpawnCircle();
        }
    }

    public ref CircleBody SpawnCircle()
    {
        ref CircleBody circle = ref circles.Add();
        circle = new CircleBody
        {
            Color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle(), 1f),
            Radius = rng.Next(1, 4) / 2.0,
            Density = 250f,
            trail = new Trail(512),
        };
        circle.Transform.Position = rng.NextVector2(new Vector2(-1500, -1000), new Vector2(1500, 0));

        circle.RigidBody.Velocity = new Double2(50, -50);
        circle.RigidBody.AngularVelocity = 8;
        circle.RigidBody.Torque = -150;

        circle.CalculateMass();

        return ref circle;
    }

    public void Update(in InputState input, in FrameTime time)
    {
        double deltaTime = 1 / 60.0 * TimeScale;
        FixedUpdate(deltaTime);
        TotalTime += deltaTime;

        ImGui.Begin("Physics");
        ImGuiPhysics();
        ImGui.End();

        ImGui.Begin("Labels");
        ImGuiLabels();
        ImGui.End();

        ImGui.Begin("Lines");
        ImGuiLines();
        ImGui.End();
    }

    private void ImGuiPhysics()
    {
        Vector2 gravity = (Vector2) Gravity;
        ImGui.InputFloat2("Gravity", ref gravity);
        Gravity = gravity;

        ImGui.Checkbox("Velocity", ref _enableVelocity);
        ImGui.Checkbox("Angular", ref _enableAngular);
    }

    private void ImGuiLabels()
    {
        ImGui.Checkbox("Position", ref _labelPosition);
        ImGui.Checkbox("Angle", ref _labelAngle);
        ImGui.Checkbox("Radius", ref _labelRadius);
        ImGui.Checkbox("Density", ref _labelDensity);
        ImGui.Checkbox("Velocity", ref _labelVelocity);
        ImGui.Checkbox("Angular", ref _labelAngular);
        ImGui.Checkbox("Mass", ref _labelMass);
        ImGui.Checkbox("Inertia", ref _labelInertia);
        ImGui.Checkbox("Torque", ref _labelTorque);
    }

    private void ImGuiLines()
    {
        ImGui.Checkbox("Trails", ref _lineTrail);
        ImGui.Checkbox("Velocity", ref _lineVelocity);
        ImGui.Checkbox("Angle", ref _lineAngle);
        ImGui.Checkbox("Forward", ref _lineForward);
    }

    public void FixedUpdate(double deltaTime)
    {
        foreach (ref CircleBody circle in circles.AsSpan())
        {
            if (_enableVelocity)
                circle.RigidBody.IntegrateVelocity(Gravity, deltaTime);

            if (_enableAngular)
                circle.RigidBody.IntegrateAngular(deltaTime);
        }

        foreach (ref CircleBody circle in circles.AsSpan())
        {
            if (_enableVelocity)
                circle.RigidBody.IntegrateVelocity(ref circle.Transform, Gravity, deltaTime);

            if (_enableAngular)
                circle.RigidBody.IntegrateAngular(ref circle.Transform, deltaTime);
        }

        if (_lineTrail)
        {
            foreach (ref CircleBody circle in circles.AsSpan())
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
        foreach (ref CircleBody circle in circles.AsSpan())
        {
            spriteBatch.DrawCircle(
                (Vector2) circle.Transform.Position,
                scale * (float) circle.Radius,
                (int) (scale * Math.Max(8, (float) circle.Radius / 2f)),
                circle.Color,
                8f);
        }

        if (_lineTrail)
        {
            foreach (ref CircleBody circle in circles.AsSpan())
            {
                circle.trail.Draw(spriteBatch, circle.Color, scale * (float) circle.Radius / 2f);
            }
        }

        if (_lineAngle)
        {
            foreach (ref CircleBody circle in circles.AsSpan())
            {
                Vector2 origin = (Vector2) circle.Transform.Position;

                (float sin, float cos) = MathF.SinCos((float) circle.Transform.Rotation);
                Vector2 dir = new(cos, sin);

                float radius = scale * (float) circle.Radius;
                Vector2 edge = origin + new Vector2(radius - 1) * dir;

                float width = MathF.Min(radius, 4f);
                spriteBatch.DrawLine(origin, edge, Color.Blue, width);
            }
        }

        if (_lineForward)
        {
            foreach (ref CircleBody circle in circles.AsSpan())
            {
                Vector2 origin = (Vector2) circle.Transform.Position;
                Vector2 velocity = (Vector2) circle.RigidBody.Velocity;

                float angle = MathF.Atan2(velocity.Y, velocity.X);
                (float sin, float cos) = MathF.SinCos(angle);
                Vector2 dir = new(cos, sin);

                float radius = scale * (float) circle.Radius;
                Vector2 edge = origin + new Vector2(radius) * dir;

                float width = MathF.Min(radius, 4f);
                spriteBatch.DrawLine(edge - dir * 2, edge + dir * 8f, Color.Aqua, width);
            }
        }

        if (_lineVelocity)
        {
            foreach (ref CircleBody circle in circles.AsSpan())
            {
                Vector2 origin = (Vector2) circle.Transform.Position;
                Vector2 end = (Vector2) circle.RigidBody.Velocity;
                spriteBatch.DrawLine(origin, origin + end, Color.Green, 2f);
            }
        }

        StringBuilder builder = _strBuilder;

        foreach (ref CircleBody circle in circles.AsSpan())
        {
            builder.Clear();

            if (_labelPosition)
                builder.AppendLine(NumberFormatInfo.InvariantInfo, $"P {circle.Transform.Position:0.0}");
            if (_labelAngle)
                builder.AppendLine(NumberFormatInfo.InvariantInfo, $"A {circle.Transform.Rotation:0.00}");
            if (_labelRadius)
                builder.AppendLine(NumberFormatInfo.InvariantInfo, $"R {circle.Radius:0.0}");
            if (_labelDensity)
                builder.AppendLine(NumberFormatInfo.InvariantInfo, $"D {circle.Density:0.0}");
            if (_labelVelocity)
                builder.AppendLine(NumberFormatInfo.InvariantInfo, $"V {circle.RigidBody.Velocity:0.0}");
            if (_labelAngular)
                builder.AppendLine(NumberFormatInfo.InvariantInfo, $"S {circle.RigidBody.AngularVelocity:0.00}");
            if (_labelMass)
                builder.AppendLine(NumberFormatInfo.InvariantInfo, $"M {1.0 / circle.RigidBody.InverseMass:0.0}");
            if (_labelInertia)
                builder.AppendLine(NumberFormatInfo.InvariantInfo, $"I {1.0 / circle.RigidBody.InverseInertia:0.0}");
            if (_labelTorque)
                builder.AppendLine(NumberFormatInfo.InvariantInfo, $"T {circle.RigidBody.Torque:0.00}");

            if (builder.Length <= 0)
                continue;

            Vector2 origin = (Vector2) circle.Transform.Position;
            spriteBatch.DrawString(
                assets.Font_Consolas, builder, origin + new Vector2((float) circle.Radius * scale + 4, -8),
                circle.Color, 0, new Vector2(), new Vector2(0.5f), SpriteFlip.None, 0);
        }
    }
}
