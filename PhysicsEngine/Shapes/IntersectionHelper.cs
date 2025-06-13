using System.Runtime.CompilerServices;

namespace PhysicsEngine.Shapes;

public static class IntersectionHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntersectionResult MakeResult(bool overlaps, bool cuts)
    {
        int overlapBit = overlaps ? 1 : 0;
        int cutBit = overlapBit & (cuts ? 1 : 0);
        return (IntersectionResult) (overlapBit | (cutBit << 1));
    }
}
