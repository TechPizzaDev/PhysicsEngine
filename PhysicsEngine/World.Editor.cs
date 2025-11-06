using System;
using System.Globalization;
using System.Numerics;
using Hexa.NET.ImGui;
using PhysicsEngine.Collections;
using PhysicsEngine.Drawing;
using PhysicsEngine.Memory;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine;

public partial class World
{
    public bool UseDegreesInput = true;

    private EnumMap<ShapeKind, EditorProxy> _editors = new();

    #region Helpers

    public bool InputAngle(ReadOnlySpan<char> label, ref double angle)
    {
        return ExGui.InputAngle(label, ref angle, UseDegreesInput);
    }

    private string GetEditorLabel(in ShapeLocation location)
    {
        return $"Editor ({location.Kind} #{location.Id})";
    }

    private static int IndexOf(in ShapeLocation location, ReadOnlySpan<ShapeEditor> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Location == location)
            {
                return i;
            }
        }
        return -1;
    }

    private void OpenEditor(in ShapeLocation location)
    {
        int i = IndexOf(location, _openEditors.AsSpan());
        if (i == -1)
        {
            _openEditors.Add() = new ShapeEditor(_nextEditorId++, location);
        }
        else
        {
            FocusEditor(location);
        }
    }

    private void FocusEditor(in ShapeLocation location)
    {
        string label = GetEditorLabel(location);
        ImGui.SetWindowCollapsed(label, false);
        ImGui.SetWindowFocus(label);
    }

    #endregion

    private void ImGuiShapeEditors()
    {
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            _objectLocations.Clear();
            _physics.GetObjectsInRange(_mousePosition, HoverRadius, _objectLocations);
            if (_objectLocations.Count > 1)
            {
                ImGui.OpenPopup("hover_multiple_shape");
            }
            else if (_objectLocations.Count == 1)
            {
                OpenEditor(_objectLocations.Get(0));
            }
        }

        if (ImGui.BeginPopupContextWindow("hover_multiple_shape"))
        {
            foreach (ref ShapeLocation location in _objectLocations.AsSpan())
            {
                if (ImGui.MenuItem($"{location.Kind} #{location.Id}"))
                {
                    OpenEditor(location);
                }
            }
            ImGui.EndPopup();
        }

        CultureInfo culture = CultureInfo.InvariantCulture;
        for (int i = 0; i < _openEditors.Count; i++)
        {
            ref ShapeEditor editor = ref _openEditors.Get(i);
            ref readonly ShapeLocation location = ref editor.Location;

            ImGui.PushID(editor.Id);
            ImGui.SetNextWindowPos(ImGui.GetMousePos(), ImGuiCond.FirstUseEver);
            bool open = true;
            if (ImGui.Begin(GetEditorLabel(location), ref open))
            {
                ImGuiShapeEditor(location, culture);
            }
            ImGui.End();
            ImGui.PopID();

            if (!open)
            {
                _openEditors.RemoveAt(i);
            }
        }

        if (ImGui.Begin($"Editors ({_openEditors.Count})###editors"))
        {
            if (ImGui.Button("Close all"))
            {
                _openEditors.Clear();
            }

            for (int i = 0; i < _openEditors.Count; i++)
            {
                ref ShapeEditor editor = ref _openEditors.Get(i);
                ShapeLocation location = editor.Location;

                ImGui.PushID(editor.Id);
                if (ImGui.SmallButton("x"))
                {
                    _openEditors.RemoveAt(i);
                }
                ImGui.SameLine();
                if (ImGui.MenuItem($"{location.Kind} #{location.Id}"))
                {
                    FocusEditor(location);
                }
                ImGui.PopID();
            }
        }
        ImGui.End();
    }

    public void SetupEditorProxies()
    {
        _editors.Fill((in ShapeLocation location, CultureInfo _) =>
        {
            ImGui.Text("Not implemented: " + location.Kind);
        });

        _editors[ShapeKind.None] = (in ShapeLocation location, CultureInfo _) =>
        {
            if (location.Kind != ShapeKind.None)
            {
                throw new ArgumentException("", nameof(location));
            }
            ImGui.Text("Empty");
        };

        void Set<T>(EditorAction<T> action)
            where T : IShapeId
        {
            _editors[T.Kind] = MakeEditorProxy(T.Kind, _physics.GetStorage<T>(), action);
        }

        Set<CircleBody>(ImGuiCircleEditor);
        Set<PlaneBody2D>(ImGuiPlaneEditor);
        Set<ExplosionBody2D>(ImGuiExplosionEditor);
        Set<WindZone>(ImGuiWindZoneEditor);
        Set<FluidZone>(ImGuiFluidZoneEditor);
    }

    private static EditorProxy MakeEditorProxy<T>(ShapeKind kind, Storage<T> storage, EditorAction<T> action)
        where T : IShapeId
    {
        return (in ShapeLocation location, CultureInfo culture) =>
        {
            if (location.Kind != kind)
            {
                throw new ArgumentException("", nameof(location));
            }
            FindEditor(location, storage.AsSpan(), culture, action);
        };
    }

    private void ImGuiShapeEditor(in ShapeLocation location, CultureInfo culture)
    {
        EditorProxy? proxy = _editors[location.Kind];
        if (proxy != null)
        {
            proxy.Invoke(location, culture);
            return;
        }

        throw new NotSupportedException($"Unsupported kind of shape: {location.Kind}");
    }

    private delegate void EditorProxy(in ShapeLocation location, CultureInfo culture);

    private delegate void EditorAction<T>(ref T item, CultureInfo culture);

    private static void FindEditor<T>(in ShapeLocation location, Span<T> span, CultureInfo culture, EditorAction<T> action)
        where T : IShapeId
    {
        int index = BodyHelper.IndexOf(location.Id, span);
        action.Invoke(ref span[index], culture);
    }

    private void ImGuiCircleEditor(ref CircleBody circle, CultureInfo culture)
    {
        if (ExGui.DragScalar("Radius", ref circle.Radius) |
            ExGui.DragScalar("Density", ref circle.Density))
        {
            circle.CalculateMass();
        }

        Vector3 color = circle.Color.ToVector3();
        ImGui.ColorEdit3("Color", ref color, ImGuiColorEditFlags.None);
        circle.Color.FromVector(color);

        ImGuiCollisionMaskEditor(ref circle.CollisionMask);

        ImGuiTransformEditor(ref circle.Transform, culture);
        ImGuiRigidBodyEditor(ref circle.RigidBody, culture);
    }

    private void ImGuiPlaneEditor(ref PlaneBody2D plane, CultureInfo culture)
    {
        Double2 normal = plane.Data.Normal;
        bool normalChanged = ExGui.DragDouble2("Normal", ref normal);

        if (ImGui.Button("Normalize"))
        {
            normal = normal.Normalize();
            normalChanged = true;
        }

        double d = plane.Data.D;
        if (normalChanged | ExGui.DragScalar("D", ref d))
        {
            plane.Data = new Plane2D(normal, d);
        }

        ImGuiCollisionMaskEditor(ref plane.CollisionMask);
    }

    private void ImGuiExplosionEditor(ref ExplosionBody2D explosion, CultureInfo culture)
    {
        ImGui.Checkbox("Apply", ref explosion.ShouldApply);

        ExGui.DragScalar("Radius", ref explosion.Radius);
        ExGui.DragScalar("Force", ref explosion.Force);
        ExGui.DragScalar("Time", ref explosion.Time);
        ExGui.DragScalar("Interval", ref explosion.Interval);

        ImGuiTransformEditor(ref explosion.Transform, culture);
    }

    private void ImGuiWindZoneEditor(ref WindZone zone, CultureInfo culture)
    {
        ExGui.DragBound2("Bound", ref zone.Bounds);
        ExGui.DragDouble2("Direction", ref zone.Direction);
        ExGui.DragScalar("Speed", ref zone.Speed);
        ExGui.DragScalar("Density", ref zone.Density);
        ExGui.DragScalar("Drag", ref zone.Drag);

        ImGui.SeparatorText("Turbulence");

        ExGui.DragScalar("Angle", ref zone.TurbulenceAngle);
        ExGui.DragScalar("Intensity", ref zone.TurbulenceIntensity);
        ExGui.DragDouble2("Scale", ref zone.TurbulenceScale);
        ExGui.DragScalar("Depth", ref zone.TurbulenceDepth);
        ExGui.DragScalar("Seed", ref zone.TurbulenceSeed);
        ExGui.DragScalar("Time", ref zone.Time);
    }

    private void ImGuiFluidZoneEditor(ref FluidZone zone, CultureInfo culture)
    {
        ExGui.DragBound2("Bound", ref zone.Bounds);
        ExGui.DragScalar("Density", ref zone.Density);
    }

    private void ImGuiCollisionMaskEditor(ref CollisionMask mask)
    {
        ReadOnlySpan<byte> label = "CollisionMask"u8;
        ReadOnlySpan<byte> popup_id = "collision_mask_editor"u8;

        float item_w = ImGui.CalcItemWidth();
        ImGuiStylePtr style = ImGui.GetStyle();

        Vector2 pos = ImGui.GetCursorScreenPos();
        Vector2 label_size = ImGui.CalcTextSize(label, true);

        ImGui.InvisibleButton(label, new Vector2(item_w - style.FramePadding.X, label_size.Y), ImGuiButtonFlags.EnableNav);
        if (ImGui.IsItemActive() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            ImGui.OpenPopup(popup_id);
        }

        bool held = ImGui.IsItemActive();
        bool hovered = ImGui.IsItemHovered();

        uint frame_col = ImGui.GetColorU32((held && hovered)
            ? ImGuiCol.FrameBgActive
            : (hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg));
        ExGui.RenderFrame(pos, pos + new Vector2(item_w, label_size.Y), frame_col, true, style.FrameRounding);

        ImGui.SameLine(style.FramePadding.X * 2f);
        ImGui.TextAligned(0.5f, item_w, $"0x{(uint) mask:X}");

        ImGui.SameLine(item_w + style.FramePadding.X * 3f);
        ImGui.Text(label);

        if (ImGui.BeginPopup(popup_id))
        {
            ImGuiCollisionMaskTable(label, ref mask);
            ImGui.EndPopup();
        }
    }

    private void ImGuiCollisionMaskTable(ReadOnlySpan<byte> label, ref CollisionMask mask)
    {
        var flags =
            ImGuiTableFlags.BordersInner |
            ImGuiTableFlags.NoPadInnerX |
            ImGuiTableFlags.NoHostExtendX |
            ImGuiTableFlags.PreciseWidths |
            ImGuiTableFlags.SizingFixedFit;

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0f, 0f));
        if (ImGui.BeginTable(label, 9, flags))
        {
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers, 20f);

            ImGui.TableSetColumnIndex(0);
            if (ImGui.Button($"{FontAwesome7.Hurricane.ToString()}##flip"))
            {
                mask = ~mask;
            }

            for (int col = 0; col < 8; col++)
            {
                ImGui.TableSetColumnIndex(col + 1);
                ImGui.AlignTextToFramePadding();
                ImGui.TextDisabled(" " + col);
            }

            int idx = 0;
            for (int row = 0; row < 4; row++)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.TextDisabled($"{row * 8} ");

                for (int col = 0; col < 8; col++)
                {
                    ImGui.TableSetColumnIndex(col + 1);

                    var bit = (CollisionMask) (1u << idx);
                    bool val = (mask & bit) == bit;
                    ImGui.Checkbox($"##{idx}", ref val);
                    if (ImGui.BeginItemTooltip())
                    {
                        ImGui.Text($"Bit {idx}");
                        ImGui.EndTooltip();
                    }

                    if (val)
                        mask |= bit;
                    else
                        mask &= ~bit;

                    idx++;
                }
            }
            ImGui.EndTable();
        }
        ImGui.PopStyleVar();
    }

    private void ImGuiTransformEditor(ref Transform2D transform, CultureInfo culture)
    {
        ImGui.SeparatorText("Transform");

        ExGui.DragDouble2("Position", ref transform.Position);
        InputAngle("Rotation", ref transform.Rotation);
    }

    private void ImGuiRigidBodyEditor(ref RigidBody2D body, CultureInfo culture)
    {
        ImGui.SeparatorText("RigidBody");

        ExGui.DragScalar("SkipFrames", ref body.SkipFrames);
        if (ImGui.BeginItemTooltip())
        {
            ImGui.Text($"dt = {TimeStep * (body.SkipFrames + 1):g4} s");
            ImGui.EndTooltip();
        }

        ExGui.DragScalar("CurrentFrame", ref body.CurrentFrame);

        ExGui.DragScalar("Torque", ref body.Torque);
        ExGui.DragScalar("AngularVelocity", ref body.AngularVelocity);
        ExGui.DragScalar("RestitutionCoeff", ref body.RestitutionCoeff);

        ExGui.DragDouble2("Velocity", ref body.Velocity);
        ExGui.DragDouble2("Force", ref body.Force);

        ImGui.LabelText("Mass", string.Create(culture, $"{1.0 / body.InverseMass:g4} kg"));
        ImGui.LabelText("Inertia", string.Create(culture, $"{1.0 / body.InverseInertia:g4} kg·m²"));
    }
}
