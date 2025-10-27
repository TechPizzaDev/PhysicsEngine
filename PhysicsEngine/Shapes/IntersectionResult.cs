using System;

namespace PhysicsEngine.Shapes;

[Flags]
public enum IntersectionResult
{
    None = 0,
    Overlaps = 0b01,
    Cuts = 0b10,
    Any = Overlaps | Cuts,
}
