using System;

namespace PhysicsEngine.Memory;

public interface ISpanOwner : IDisposable
{
    ReadOnlySpan<byte> Span { get; }
}
