using System;
using System.Runtime.InteropServices;
using System.Threading;
using PhysicsEngine.Memory;

namespace PhysicsEngine;

public unsafe struct NativeOwner(byte* data, int length) : ISpanOwner
{
    private nint _data = (nint) data;

    public readonly ReadOnlySpan<byte> Span => new((byte*) _data, length);

    public void Dispose()
    {
        nint data = Interlocked.Exchange(ref _data, 0);
        if (data != 0)
        {
            NativeMemory.Free((byte*) data);
        }
    }
}
