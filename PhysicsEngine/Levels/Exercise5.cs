using System;
using System.Numerics;
using Hexa.NET.ImGui;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class Exercise5 : ExerciseWorld
{
    public struct CircleState
    {
        public BodyId Id;
        public double Torque;
        public double ExpectedTimeOfFlip;

        public double TimeToFlip;
        public double VelocityForTimeCorrection;

        public double WorldTimeOfFlip;

        /// <summary>
        /// May be inaccurate if <see cref="RigidBody2D.AngularVelocity"/> 
        /// was affected by external sources during skipped time.
        /// </summary>
        public double CorrectedTimeOfFlip;
    }

    protected CircleState[] _circles;

    public Exercise5(int count, Random random) : base(random)
    {
        Physics.Gravity = new Double2(0);

        _lineAngle = true;
        _labelAngle = true;
        _labelAngular = true;

        _circles = new CircleState[count];
        for (int i = 0; i < _circles.Length; i++)
        {
            ref CircleState cs = ref _circles[i];
            ref CircleBody circle = ref CreateCircle(out cs, i, -150);
            ref RigidBody2D body = ref circle.RigidBody;
            cs.ExpectedTimeOfFlip = TimeFromAngularVelocity(0, body.AngularVelocity, cs.Torque, body.InverseInertia);
        }
    }

    public Exercise5(Random random) : this(1, random)
    {
    }

    public override (Vector2? Position, float? Scale) GetInitialCameraState()
    {
        var (_, Scale) = base.GetInitialCameraState();
        return (new Vector2(0, _circles.Length * 2f - 2f), Scale);
    }

    public virtual ref CircleBody CreateCircle(out CircleState state, int index, double torque)
    {
        ref CircleBody circle = ref Add(new CircleBody()
        {
            Radius = 0.5 * Math.Pow(2, index / 4.0),
            Density = 250,
            Position = new Double2(0, index * -4)
        });
        circle.CalculateMass();

        ref RigidBody2D body = ref circle.RigidBody;
        body.AngularVelocity = 8 * Math.PI;

        state = new CircleState()
        {
            Id = circle.Id,
            Torque = torque,
        };
        return ref circle;
    }

    public override void Update(in UpdateState state)
    {
        base.Update(state);

        if (ImGui.Begin("States"))
        {
            foreach (ref CircleState cs in _circles.AsSpan())
            {
                ImGui.SetNextItemOpen(true, ImGuiCond.Once);
                if (ImGui.TreeNodeEx($"Circle #{cs.Id}", ImGuiTreeNodeFlags.Framed))
                {
                    ImGuiCircleState(ref cs);
                    ImGui.TreePop();
                    ImGui.Spacing();
                }
            }
        }
        ImGui.End();
    }

    private void ImGuiCircleState(ref CircleState state)
    {
        ImGui.Text($"Expected time of flip: {state.ExpectedTimeOfFlip:g4}s");
        ImGui.Text($"Corrected time of flip: {state.CorrectedTimeOfFlip:g4}s");
        ImGui.Text($"World time of flip: {state.WorldTimeOfFlip:g4}s");
        ImGui.Text($"Time to flip: {state.TimeToFlip:g4}s");
    }

    public override void FixedUpdate(in UpdateState state, double deltaTime)
    {
        foreach (ref CircleState cs in _circles.AsSpan())
        {
            ref CircleBody circle = ref Physics.Get<CircleBody>(cs.Id);
            ref RigidBody2D body = ref circle.RigidBody;
            body.ApplyTorque(cs.Torque);

            cs.TimeToFlip = TimeFromAngularVelocity(0, body.AngularVelocity, cs.Torque, body.InverseInertia);
            cs.VelocityForTimeCorrection = body.AngularVelocity;
        }

        base.FixedUpdate(state, deltaTime);

        foreach (ref CircleState cs in _circles.AsSpan())
        {
            double v0 = cs.VelocityForTimeCorrection;
            if (v0 <= 0)
                continue;

            ref CircleBody circle = ref Physics.Get<CircleBody>(cs.Id);
            ref RigidBody2D body = ref circle.RigidBody;

            double v1 = body.AngularVelocity;
            if (v1 > 0)
                continue;
            
            // Find the in-between point in this frame where velocity became zero.
            double mid = MathG.InverseLerp(v0, v1, 0);

            // Account for skipped frames.
            double overflow = body.SkipFrames - body.CurrentFrame;
            double underflow = mid * (overflow + 1.0);

            cs.WorldTimeOfFlip = TotalTime;
            cs.CorrectedTimeOfFlip = TotalTime + (underflow - overflow) * deltaTime;
        }
    }

    public static double TimeFromAngularVelocity(
        double v, double w0, double torque, double inv_inertia)
    {
        return (v - w0) / (torque * inv_inertia);
    }

    public static double AngularVelocityOverConstantTorque(
        double w0, double torque, double inv_inertia, double time)
    {
        return w0 + (torque * inv_inertia) * time;
    }
}
