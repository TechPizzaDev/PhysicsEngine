using System;

namespace PhysicsEngine.Shapes;

[Flags]
public enum CollisionMask : uint
{
    None = 0,
    L0 = 1u << 0,
    L1 = 1u << 1,
    L2 = 1u << 2,
    L3 = 1u << 3,
    L4 = 1u << 4,
    L5 = 1u << 5,
    L6 = 1u << 6,
    L7 = 1u << 7,
    L8 = 1u << 8,
    L9 = 1u << 9,
    L10 = 1u << 10,
    L11 = 1u << 11,
    L12 = 1u << 12,
    L13 = 1u << 13,
    L14 = 1u << 14,
    L15 = 1u << 15,
    L16 = 1u << 16,
    L17 = 1u << 17,
    L18 = 1u << 18,
    L19 = 1u << 19,
    L20 = 1u << 20,
    L21 = 1u << 21,
    L22 = 1u << 22,
    L23 = 1u << 23,
    L24 = 1u << 24,
    L25 = 1u << 25,
    L26 = 1u << 26,
    L27 = 1u << 27,
    L28 = 1u << 28,
    L29 = 1u << 29,
    L30 = 1u << 30,
    L31 = 1u << 31,
    All = ~0u,
}
