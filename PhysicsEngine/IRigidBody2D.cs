namespace PhysicsEngine;

public interface IRigidBody2D
{
    Double2 Velocity { get; }

    double InverseMass { get; }

    double RestitutionCoeff { get; }

    void ApplyImpulse(Double2 impulse, Double2 contactVector);
}
