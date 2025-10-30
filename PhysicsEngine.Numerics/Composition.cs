using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PhysicsEngine.Numerics;

public static class Composition
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<byte> ApplyAlpha(Vector128<byte> x, Vector128<ushort> a)
    {
        Vector256<ushort> offset = Vector256.Create(0x0080_0080).AsUInt16();

        Vector256<ushort> x16 = Avx2.IsSupported
            ? Avx2.ConvertToVector256Int16(x).AsUInt16()
            : Vector256.Create(Vector128.WidenLower(x), Vector128.WidenUpper(x));

        Vector256<ushort> a0 = x16 * Vector256.Create(a, a);
        Vector256<ushort> a1 = ((a0 + (a0 >> 8) + offset) >> 8).AsUInt16();

        return Avx512BW.VL.IsSupported
            ? Avx512BW.VL.ConvertToVector128Byte(a1)
            : Vector128.Narrow(a1.GetLower(), a1.GetUpper());
    }
}
