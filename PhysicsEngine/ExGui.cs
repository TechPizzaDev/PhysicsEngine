using System;
using ImGuiNET;
using PhysicsEngine.Numerics;

namespace PhysicsEngine;

public static class ExGui
{
    public const ImGuiSliderFlags DefaultSliderFlags = ImGuiSliderFlags.NoRoundToFormat;

    public static unsafe bool DragScalars<T>(
        ReadOnlySpan<char> label,
        Span<T> values,
        float speed,
        T min,
        T max,
        ReadOnlySpan<char> format,
        ImGuiSliderFlags flags)
        where T : unmanaged
    {
        if (format.IsEmpty)
            format = "%.4g";

        fixed (T* local = values)
        {
            ImGuiDataType ty = TypeToImGui(typeof(T));
            return ImGui.DragScalarN(label, ty, (nint) local, values.Length, speed, (nint) (&min), (nint) (&max), format, flags);
        }
    }

    public static unsafe bool DragScalars<T>(
        ReadOnlySpan<char> label,
        Span<T> values,
        ReadOnlySpan<char> format,
        ImGuiSliderFlags flags)
        where T : unmanaged
    {
        if (format.IsEmpty)
            format = "%.4g";

        fixed (T* local = values)
        {
            ImGuiDataType ty = TypeToImGui(typeof(T));
            return ImGui.DragScalarN(label, ty, (nint) local, values.Length, 1f, 0, 0, format, flags);
        }
    }

    public static ImGuiDataType TypeToImGui(Type type)
    {
        if (type == typeof(double))
            return ImGuiDataType.Double;
        if (type == typeof(float))
            return ImGuiDataType.Float;
        if (type == typeof(int))
            return ImGuiDataType.S32;

        static ImGuiDataType ThrowUnsupported() => throw new NotSupportedException();
        return ThrowUnsupported();
    }

    public static bool DragScalar<T>(
        ReadOnlySpan<char> label,
        ref T value,
        float speed,
        T min,
        T max,
        ReadOnlySpan<char> format = default,
        ImGuiSliderFlags flags = DefaultSliderFlags)
        where T : unmanaged
    {
        return DragScalars(label, new Span<T>(ref value), speed, min, max, format, flags);
    }

    public static bool DragScalar<T>(
        ReadOnlySpan<char> label,
        ref T value,
        ReadOnlySpan<char> format = default,
        ImGuiSliderFlags flags = DefaultSliderFlags)
        where T : unmanaged
    {
        return DragScalars(label, new Span<T>(ref value), format, flags);
    }

    public static bool DragDouble2(
        ReadOnlySpan<char> label,
        ref Double2 value,
        ReadOnlySpan<char> format = default,
        ImGuiSliderFlags flags = DefaultSliderFlags)
    {
        Span<double> values = stackalloc double[2];
        value.CopyTo(values);
        if (DragScalars(label, values, format, flags))
        {
            value = new Double2(values);
            return true;
        }
        return false;
    }

    public static bool DragBound2(
        ReadOnlySpan<char> label,
        ref Bound2 value,
        ReadOnlySpan<char> format = default,
        ImGuiSliderFlags flags = DefaultSliderFlags)
    {
        Double2 min = value.Min;
        Double2 max = value.Max;
        if (DragDouble2($"{label} Min", ref min, format, flags) |
            DragDouble2($"{label} Max", ref max, format, flags))
        {
            value = new Bound2(min, max);
            return true;
        }
        return false;
    }

    public static bool InputAngle(ReadOnlySpan<char> label, ref double angle, bool degrees)
    {
        bool change;
        double a = angle;
        if (degrees)
        {
            a *= 180 / Math.PI;
            change = ExGui.DragScalar(label, ref a, 1f, -360, 360, "%3.0f°", ImGuiSliderFlags.NoRoundToFormat);
            a /= 180 / Math.PI;
        }
        else
        {
            change = ExGui.DragScalar(label, ref a, 1f, -Math.PI * 2, Math.PI * 2, "%f", ImGuiSliderFlags.NoRoundToFormat);
        }
        angle = a;
        return change;
    }
}
