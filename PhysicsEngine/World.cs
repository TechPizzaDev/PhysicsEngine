using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ImGuiNET;
using MonoGame.Framework;
using MonoGame.Framework.Graphics;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine;

public partial class World
{
    private PhysicsWorld _physics;
    private uint _nextCircleId = 1;

    public Random rng;

    private StringBuilder _strBuilder = new();

    private Vector2 mousePosition;

    #region Variables

    public double TotalTime;
    public double TimeScale = 1f;

    public float HoverRadius = 5f;

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

    private bool _lineTrail = false;
    private bool _lineVelocity = false;
    private bool _lineAngle = false;
    private bool _lineForward = false;

    #endregion

    public World(Random random)
    {
        rng = random;

        _physics = new PhysicsWorld();

        SetupEditorProxies();

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
        return ref SpawnCircle(rng, _physics.GetStorage<CircleBody>());
    }

    public ref CircleBody SpawnCircle(Random rng, Storage<CircleBody> storage)
    {
        ref CircleBody circle = ref storage.Add();
        circle = new CircleBody(new BodyId(_nextCircleId++))
        {
            Color = new Color(rng.NextSingle(), rng.NextSingle(), rng.NextSingle(), 1f),
            Radius = rng.Next(20, 40),
            Density = 250f,
            trail = new Trail(512),
        };
        circle.Transform.Position = rng.NextVector2(new Vector2(-3000, 5000), new Vector2(3000, 0));

        circle.RigidBody.Velocity = new Double2(50, 50);
        circle.RigidBody.AngularVelocity = 8;
        circle.RigidBody.Torque = -150;
        circle.RigidBody.RestitutionCoeff = 0.5;

        circle.CalculateMass();

        return ref circle;
    }

    public void Update(in InputState input, in FrameTime time, Matrix4x4 inverseSceneTransform)
    {
        mousePosition = Vector2.Transform(input.NewMouseState.Position.ToVector2(), inverseSceneTransform);

        double deltaTime = 1 / 60.0 * TimeScale;
        FixedUpdate(deltaTime);
        TotalTime += deltaTime;

        ImGuiUpdate();
    }

    #region ImGui Editors

    private Storage<ShapeLocation> _objectLocations = new();
    private Storage<ShapeEditor> _openEditors = new();
    private int _nextEditorId = 1;

    public readonly struct ShapeEditor(int id, ShapeLocation location) : IShapeId
    {
        public static ShapeKind Kind => ShapeKind.Editor;

        public readonly int Id = id;
        public readonly ShapeLocation Location = location;

        BodyId IShapeId.Id { get => Location.Id; set { throw new NotSupportedException(); } }
    }

    private void ImGuiUpdate()
    {
        ImGuiShapeEditors();

        //if (ImGui.Begin("Circles"))
        //{
        //    ImGuiCircles();
        //}
        //ImGui.End();

        if (ImGui.Begin("Physics"))
        {
            ImGuiPhysics();
        }
        ImGui.End();

        if (ImGui.Begin("Labels"))
        {
            ImGuiLabels();
        }
        ImGui.End();

        if (ImGui.Begin("Lines"))
        {
            ImGuiLines();
        }
        ImGui.End();
    }

    private void ImGuiPhysics()
    {
        ImGui.PushItemWidth(80);
        ExGui.DragDouble2("Gravity", ref _physics.Gravity);

        ExGui.DragScalar("Time scale", ref TimeScale);
        ExGui.DragScalar("Error reduction", ref _physics.ErrorReduction);
        ImGui.PopItemWidth();

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

        ImGui.PushItemWidth(60);
        ImGui.SliderFloat("Wind Flow", ref _physics.WindFlowLineOpacity, 0f, 1f);
        ImGui.PopItemWidth();
    }

    #endregion

    private void IntegrateBody(double halfDt, Double2 gravity, ref CircleBody circle)
    {
        if (_enableVelocity)
        {
            circle.RigidBody.IntegrateVelocity(ref circle.Transform, gravity, halfDt);
            circle.RigidBody.IntegrateVelocity(gravity, halfDt);
        }

        if (_enableAngular)
        {
            circle.RigidBody.IntegrateAngular(halfDt);
            circle.RigidBody.IntegrateAngular(ref circle.Transform, halfDt);
        }

        circle.RigidBody.Force = default;
        circle.RigidBody.Torque = 0;
    }

    public void FixedUpdate(double deltaTime)
    {
        double halfDt = deltaTime * 0.5;

        Double2 gravity = _physics.Gravity;

        foreach (ref CircleBody circle in _physics.GetStorage<CircleBody>().AsSpan())
        {
            IntegrateBody(halfDt, gravity, ref circle);

            if (_lineTrail)
            {
                circle.trail.Update((Vector2) circle.Transform.Position);
            }
        }

        long startStamp = Stopwatch.GetTimestamp();
        _physics.FixedUpdate(deltaTime);
        TimeSpan endTime = Stopwatch.GetElapsedTime(startStamp);
        //Console.WriteLine($"Collision: {endTime.TotalMilliseconds}ms");
    }

    #region Drawing

    public void Draw(RenderPass renderPass, AssetRegistry assets, SpriteBatch spriteBatch, float scale)
    {
        if (renderPass == RenderPass.Scene)
        {
            DrawWorld(assets, spriteBatch, scale);

            _physics.DrawWorld(assets, spriteBatch, scale);

            spriteBatch.DrawCircle(mousePosition, HoverRadius, 12, Color.Red, 1 / scale);
        }
    }

    private void DrawWorld(AssetRegistry assets, SpriteBatch spriteBatch, float scale)
    {
        foreach (ref CircleBody circle in _physics.GetStorage<CircleBody>().AsSpan())
        {
            DrawCircleBody(spriteBatch, scale, circle);

            //spriteBatch.DrawRectangle(circle.Circle.Bounds.ToRectF(), Color.Red);

            if (_lineAngle)
                DrawAngle(spriteBatch, circle);

            if (_lineForward)
                DrawForward(spriteBatch, circle);

            if (_lineVelocity)
                DrawVelocity(spriteBatch, circle);

            DrawCircleDebugText(assets, spriteBatch, scale, _strBuilder, circle);
        }
    }

    private void DrawCircleBody(SpriteBatch spriteBatch, float scale, in CircleBody circle)
    {
        Vector2 center = (Vector2) circle.Transform.Position;
        float radius = (float) circle.Radius;
        int sides = int.Clamp((int) ((float) circle.Radius * scale + 3.5f), 6, 42);
        spriteBatch.DrawCircle(center, radius, sides, circle.Color, 4f);

        if (_lineTrail)
        {
            circle.trail.Draw(spriteBatch, circle.Color, (float) circle.Radius * 0.4f);
        }
    }

    private static void DrawAngle(SpriteBatch spriteBatch, in CircleBody circle)
    {
        Vector2 origin = (Vector2) circle.Transform.Position;

        (float sin, float cos) = MathF.SinCos((float) circle.Transform.Rotation);
        Vector2 dir = new(cos, sin);

        float radius = (float) circle.Radius;
        Vector2 edge = origin + new Vector2(radius - 1) * dir;

        float width = MathF.Min(radius, 4f);
        spriteBatch.DrawLine(origin, edge, Color.Blue, width);
    }

    private static void DrawForward(SpriteBatch spriteBatch, in CircleBody circle)
    {
        Vector2 origin = (Vector2) circle.Transform.Position;
        Vector2 velocity = (Vector2) circle.RigidBody.Velocity;

        float angle = MathF.Atan2(velocity.Y, velocity.X);
        (float sin, float cos) = MathF.SinCos(angle);
        Vector2 dir = new(cos, sin);

        float radius = (float) circle.Radius;
        Vector2 edge = origin + new Vector2(radius) * dir;

        float width = MathF.Min(radius, 4f);
        spriteBatch.DrawLine(edge - dir * 2, edge + dir * 8f, Color.Aqua, width);
    }

    private static void DrawVelocity(SpriteBatch spriteBatch, in CircleBody circle)
    {
        Vector2 origin = (Vector2) circle.Transform.Position;
        Vector2 end = (Vector2) circle.RigidBody.Velocity;
        spriteBatch.DrawLine(origin, origin + end, Color.Green, 2f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Append(StringBuilder builder, in CircleBody circle)
    {
        var invariant = NumberFormatInfo.InvariantInfo;

        if (_labelPosition)
            builder.AppendLine(invariant, $"P {circle.Transform.Position:0.0}");
        if (_labelAngle)
            builder.AppendLine(invariant, $"A {circle.Transform.Rotation:0.00}");
        if (_labelRadius)
            builder.AppendLine(invariant, $"R {circle.Radius:0.0}");
        if (_labelDensity)
            builder.AppendLine(invariant, $"D {circle.Density:0.0}");

        ref readonly RigidBody2D body = ref circle.RigidBody;
        if (_labelVelocity)
            builder.AppendLine(invariant, $"V {body.Velocity:0.0}");
        if (_labelAngular)
            builder.AppendLine(invariant, $"S {body.AngularVelocity:0.00}");
        if (_labelMass)
            builder.AppendLine(invariant, $"M {1.0 / body.InverseMass:0.0}");
        if (_labelInertia)
            builder.AppendLine(invariant, $"I {1.0 / body.InverseInertia:0.0}");
        if (_labelTorque)
            builder.AppendLine(invariant, $"T {body.Torque:0.00}");
    }

    private void DrawCircleDebugText(
        AssetRegistry assets, SpriteBatch spriteBatch, float scale, StringBuilder builder, in CircleBody circle)
    {
        builder.Clear();

        Append(builder, circle);

        if (builder.Length <= 0)
            return;

        Vector2 origin = (Vector2) circle.Transform.Position;
        spriteBatch.DrawString(
            assets.Font_Consolas, builder, origin + new Vector2((float) circle.Radius * scale + 4, -8),
            circle.Color, 0, new Vector2(), new Vector2(0.5f), SpriteFlip.Vertical, 0);
    }

    #endregion
}
