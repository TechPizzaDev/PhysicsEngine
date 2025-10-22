using System;
using System.Globalization;
using System.Numerics;
using ImGuiNET;
using PhysicsEngine.Shapes;

namespace PhysicsEngine;

public partial class World
{
    public bool UseDegreesInput = true;

    public bool InputAngle(ReadOnlySpan<char> label, ref double angle)
    {
        return ExGui.InputAngle(label, ref angle, UseDegreesInput);
    }

    private void ImGuiCircles()
    {
        var culture = CultureInfo.InvariantCulture;

        var circles = _physics.Circles.AsSpan();
        for (int i = 0; i < circles.Length; i++)
        {
            ImGui.PushID(i);
            if (ImGui.CollapsingHeader("Circle " + i))
            {
                ImGuiCircleEditor(ref circles[i], culture);
            }
            ImGui.PopID();
        }
    }

    private void ImGuiCircleEditor(ref CircleBody circle, CultureInfo culture)
    {
        if (ExGui.DragScalar("Radius", ref circle.Radius, "%.4g") |
            ExGui.DragScalar("Density", ref circle.Density, "%.4g"))
        {
            circle.CalculateMass();
        }

        Vector3 color = circle.Color.ToVector3();
        ImGui.ColorEdit3("Color", ref color, ImGuiColorEditFlags.None);
        circle.Color.FromVector(color);

        ImGuiTransformEditor(ref circle.Transform, culture);
        ImGuiRigidBodyEditor(ref circle.RigidBody, culture);
    }

    private void ImGuiTransformEditor(ref Transform2D transform, CultureInfo culture)
    {
        ImGui.SeparatorText("Transform");

        ExGui.DragDouble2("Position", ref transform.Position, "%.4g");
        InputAngle("Rotation", ref transform.Rotation);
    }

    private void ImGuiRigidBodyEditor(ref RigidBody2D body, CultureInfo culture)
    {
        ImGui.SeparatorText("RigidBody");

        ExGui.DragScalar("Torque", ref body.Torque, "%.4g");
        ExGui.DragScalar("AngularVelocity", ref body.AngularVelocity, "%.4g");
        ExGui.DragScalar("RestitutionCoeff", ref body.RestitutionCoeff, "%.4g");

        ExGui.DragDouble2("Velocity", ref body.Velocity, "%.4g");
        ExGui.DragDouble2("Force", ref body.Force, "%.4g");

        ImGui.LabelText("Mass", string.Create(culture, $"{1.0 / body.InverseMass:g4} kg"));
        ImGui.LabelText("Inertia", string.Create(culture, $"{1.0 / body.InverseInertia:g4} kg·m²"));
    }
}
