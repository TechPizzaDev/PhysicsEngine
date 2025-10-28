using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PhysicsEngine.Memory;

public static class ExMarshal
{
    public static unsafe NativeOwner ToUtf8(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return default;
        }

        int maxLen = Encoding.UTF8.GetMaxByteCount(span.Length);
        byte* ptr = (byte*) NativeMemory.Alloc(checked((uint) maxLen + 1));

        int written = Encoding.UTF8.GetBytes(span, new Span<byte>(ptr, maxLen));
        ptr[written] = 0;

        return new NativeOwner(ptr, written);
    }
}
