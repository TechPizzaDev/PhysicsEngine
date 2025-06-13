using System;
using MonoGame.Framework;

namespace PhysicsEngine.Shapes;

public struct CircleBody
{
    public Transform Transform;
    public RigidBody RigidBody;

    public double Radius;
    public double Density;

    public Color Color;

    public Trail trail;

    public void CalculateMass()
    {
        double mass = Math.PI * Radius * Radius * Density;
        double inertia = mass * Radius * Radius / 2;

        RigidBody.InverseMass = mass != 0 ? 1.0 / mass : 0.0;
        RigidBody.InverseInertia = inertia != 0 ? 1.0 / inertia : 0.0;
    }
}
