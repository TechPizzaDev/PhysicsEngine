using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Memory;

public static class ExGui
{
    public const ImGuiSliderFlags DefaultSliderFlags = ImGuiSliderFlags.NoRoundToFormat;

    public static unsafe Span<T> AsSpan<T>(ref this ImVector<T> vector)
        where T : unmanaged
    {
        return new Span<T>(vector.Data, vector.Size);
    }

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
        ImGuiDataType ty = TypeToImGui(typeof(T));
        if (format.IsEmpty)
            format = DefaultImGuiFormat(ty);

        using var labelU8 = ExMarshal.ToUtf8(label);
        using var formatU8 = ExMarshal.ToUtf8(format);
        fixed (T* local = values)
        {
            return ImGui.DragScalarN(
                labelU8.Span, ty, local, values.Length, speed, &min, &max, formatU8.Span, flags);
        }
    }

    public static unsafe bool DragScalars<T>(
        ReadOnlySpan<char> label,
        Span<T> values,
        ReadOnlySpan<char> format,
        ImGuiSliderFlags flags)
        where T : unmanaged
    {
        ImGuiDataType ty = TypeToImGui(typeof(T));
        if (format.IsEmpty)
            format = DefaultImGuiFormat(ty);

        using var labelU8 = ExMarshal.ToUtf8(label);
        using var formatU8 = ExMarshal.ToUtf8(format);
        fixed (T* local = values)
        {
            return ImGui.DragScalarN(
                labelU8.Span, ty, local, values.Length, 1f, null, null, formatU8.Span, flags);
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
        if (type == typeof(byte))
            return ImGuiDataType.U8;

        static ImGuiDataType ThrowUnsupported() => throw new NotSupportedException();
        return ThrowUnsupported();
    }

    public static string DefaultImGuiFormat(ImGuiDataType type)
    {
        return type switch
        {
            ImGuiDataType.Float or
            ImGuiDataType.Double => "%.4g",
            _ => "",
        };
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
            change = DragScalar(label, ref a, 1f, -360, 360, "%3.0f°", DefaultSliderFlags);
            a /= 180 / Math.PI;
        }
        else
        {
            change = DragScalar(label, ref a, 1f, -Math.PI * 2, Math.PI * 2, "%f", DefaultSliderFlags);
        }
        angle = a;
        return change;
    }

    public static void PlotHistogram(
        ReadOnlySpan<char> label, ReadOnlySpan<float> values, int values_offset,
        ReadOnlySpan<char> overlay_text, float scale_min, float scale_max, Vector2 graph_size)
    {
        using var labelU8 = ExMarshal.ToUtf8(label);
        using var overlayU8 = ExMarshal.ToUtf8(overlay_text);
        PlotHistogram(
            labelU8.Span, values, values_offset,
            overlayU8.Span, scale_min, scale_max, graph_size);
    }

    public static void PlotHistogram(
        ReadOnlySpan<byte> label, ReadOnlySpan<float> values, int values_offset,
        ReadOnlySpan<byte> overlay_text, float scale_min, float scale_max, Vector2 graph_size)
    {
        ImGui.PlotHistogram(
            label, ref MemoryMarshal.GetReference(values), values.Length, values_offset,
            overlay_text, scale_min, scale_max, graph_size);
    }

    public static void RenderFrame(Vector2 p_min, Vector2 p_max, uint fill_col, bool borders, float rounding)
    {
        var ctx = ImGui.GetCurrentContext();
        var list = ctx.CurrentWindow.DrawList;
        ImGui.AddRectFilled(list, p_min, p_max, fill_col, rounding);

        float border_size = ctx.Style.FrameBorderSize;
        if (borders && border_size > 0.0f)
        {
            uint bs_col = ImGui.GetColorU32(ImGuiCol.BorderShadow);
            ImGui.AddRect(list, p_min + new Vector2(1, 1), p_max + new Vector2(1, 1), bs_col, rounding, 0, border_size);

            uint b_col = ImGui.GetColorU32(ImGuiCol.Border);
            ImGui.AddRect(list, p_min, p_max, b_col, rounding, 0, border_size);
        }
    }
}
