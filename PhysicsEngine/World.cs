using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Numerics.Tensors;
using System.Text;
using Hexa.NET.ImGui;
using MonoGame.Framework;
using MonoGame.Framework.Graphics;
using PhysicsEngine.Collections;
using PhysicsEngine.Drawing;
using PhysicsEngine.Memory;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine;

public partial class World
{
    private Random _random;
    private PhysicsWorld _physics;

    public Random Random => _random;
    public PhysicsWorld Physics => _physics;

    private StringBuilder _strBuilder = new();

    private Vector2 _mousePosition;

    // TODO: move to GameFrame
    private Ring<float> _updateTimeRing = new(300);
    private Ring<float> _drawTimeRing = new(300);

    #region Variables

    public double TotalTime;
    public double TimeScale = 1f;

    public float HoverRadius = 5f;

    private bool _labelPosition;
    private bool _labelAngle;
    private bool _labelRadius;
    private bool _labelDensity;
    private bool _labelVelocity;
    private bool _labelAngular;
    private bool _labelMass;
    private bool _labelInertia;
    private bool _labelTorque;

    private bool _lineVelocity = false;
    private bool _lineAngle = false;
    private bool _lineForward = false;

    #endregion

    public virtual string Name => GetType().Name;

    public World(Random random)
    {
        _random = random;
        _physics = new PhysicsWorld();

        SetupEditorProxies();
    }

    public virtual (Vector2? Position, float? Scale) GetInitialCameraState() => default;

    public ref T Add<T>() where T : IShapeId, new() => ref Physics.Add<T>();

    public ref T Add<T>(T value) where T : IShapeId => ref Physics.Add(value);

    public virtual void Update(in UpdateState state)
    {
        _mousePosition = Vector2.Transform(state.Input.MousePosition, state.InverseSceneTransform);

        double deltaTime = 1 / 60.0 * TimeScale;
        FixedUpdate(state, deltaTime);
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

    public virtual void ImGuiUpdate()
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

        if (ImGui.Begin("Stats"))
        {
            ImGuiStats();
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

        ImGui.Checkbox("Velocity", ref _physics.EnableVelocity);
        ImGui.Checkbox("Angular", ref _physics.EnableAngular);
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
        ImGui.Checkbox("Trails", ref _physics.LineTrail);
        ImGui.Checkbox("Velocity", ref _lineVelocity);
        ImGui.Checkbox("Angle", ref _lineAngle);
        ImGui.Checkbox("Forward", ref _lineForward);

        ImGui.PushItemWidth(60);
        ImGui.SliderFloat("Wind Flow", ref _physics.WindFlowLineOpacity, 0f, 1f);
        ImGui.PopItemWidth();
    }

    private void ImGuiStats()
    {
        ImGui.PushID("##fixed_update_time"u8);
        ImGui.Text("Fixed Update Time (ms)"u8);
        ImGuiPlot(_updateTimeRing.GetFullSpan(), _updateTimeRing.Tail, new Vector2(300, 80));
        ImGui.PopID();

        ImGui.NewLine();

        ImGui.PushID("##draw_time"u8);
        ImGui.Text("Draw Time (ms)"u8);
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.5f, 1f, 0.5f, 1f));
        ImGuiPlot(_drawTimeRing.GetFullSpan(), _drawTimeRing.Tail, new Vector2(300, 80));
        ImGui.PopStyleColor();
        ImGui.PopID();
    }

    private void ImGuiPlot(Span<float> values, int offset, Vector2 size)
    {
        float min = TensorPrimitives.Min(values);
        float avg = TensorPrimitives.Average<float>(values);
        float max = TensorPrimitives.Max(values);

        float padding = ImGui.GetStyle().FramePadding.X * 2;
        Vector2 graph_size = new Vector2(padding, 0) + size;
        ExGui.PlotHistogram("##values"u8, values, offset, null, min, max, graph_size);

        ImGui.BulletText($"Min: {min:0.00}");
        ImGui.SameLine();
        ImGui.BulletText($"Avg: {avg:0.00}");
        ImGui.SameLine();
        ImGui.BulletText($"Max: {max:0.00}");
    }

    #endregion

    public virtual void FixedUpdate(in UpdateState state, double deltaTime)
    {
        long startStamp = Stopwatch.GetTimestamp();
        _physics.FixedUpdate(state, deltaTime);
        TimeSpan endTime = Stopwatch.GetElapsedTime(startStamp);
        _updateTimeRing.PushBack((float) endTime.TotalMilliseconds);
    }

    #region Drawing

    public virtual void Draw(in DrawState state)
    {
        if (state.RenderPass == RenderPass.Scene)
        {
            long startStamp = Stopwatch.GetTimestamp();

            DrawWorld(state);

            _physics.DrawWorld(state);

            state.SpriteBatch.DrawCircle(_mousePosition, HoverRadius, 16, Color.Red, 1 / state.Scale);

            TimeSpan endTime = Stopwatch.GetElapsedTime(startStamp);
            _drawTimeRing.PushBack((float) endTime.TotalMilliseconds);
        }
    }

    protected virtual void DrawWorld(in DrawState state)
    {
        SpriteBatch spriteBatch = state.SpriteBatch;

        foreach (ref CircleBody circle in _physics.GetStorage<CircleBody>().AsSpan())
        {
            RectangleF fullRect = circle.GetBounds().ToRectF();
            RectangleF rect = RectangleF.Intersection(fullRect, state.WorldViewport);
            bool visible = !rect.IsEmpty;

            DrawCircleBody(state, circle, visible);
            if (!visible)
            {
                continue;
            }

            //spriteBatch.DrawRectangle(circle.Circle.Bounds.ToRectF(), Color.Red);

            if (_lineAngle)
                DrawAngle(spriteBatch, circle);

            if (_lineForward)
                DrawForward(spriteBatch, circle);

            if (_lineVelocity)
                DrawVelocity(spriteBatch, circle);

            DrawCircleDebugText(state, _strBuilder, circle);
        }
    }

    private void DrawCircleBody(in DrawState state, in CircleBody circle, bool visible)
    {
        Vector2 center = (Vector2) circle.Transform.Position;

        float scale = state.Scale;
        float inv_scale = 1f / scale;
        float inv_final_scale = 1f / state.FinalScale;

        float radius = (float) circle.Radius;
        if (visible)
        {
            if (radius < inv_scale)
            {
                state.SpriteBatch.DrawPoint(center, circle.Color, inv_final_scale);
            }
            else
            {
                int sides = int.Clamp((int) ((float) circle.Radius * scale + 3.5f), 6, 42);
                float thick = MathHelper.Clamp(4f / Math.Max(1f, scale), scale, radius * 2f);
                state.SpriteBatch.DrawCircle(center, radius, sides, circle.Color, thick);
            }
        }

        if (_physics.LineTrail)
        {
            float width = MathHelper.Clamp(radius * 0.4f * inv_final_scale, inv_final_scale, radius);
            circle.trail?.DrawLines(state.SpriteBatch, circle.Color, width);
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

    protected virtual void Append(StringBuilder builder, in CircleBody circle)
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

    private void DrawCircleDebugText(in DrawState state, StringBuilder builder, in CircleBody circle)
    {
        builder.Clear();

        Append(builder, circle);

        if (builder.Length <= 0)
            return;

        Vector2 origin = (Vector2) circle.Transform.Position;
        Vector2 pos = origin + new Vector2((float) circle.Radius * state.Scale + 4, -8);

        state.SpriteBatch.DrawString(
            state.Assets.Font_Consolas, builder, pos,
            circle.Color, 0, new Vector2(), new Vector2(0.5f), SpriteFlip.Vertical, 0);
    }

    #endregion
}
