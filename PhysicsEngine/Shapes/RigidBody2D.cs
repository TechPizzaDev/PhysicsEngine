using System;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Shapes;

public struct RigidBody2D : IRigidBody2D
{
    public Double2 Velocity;
    public Double2 Force;
    public double Torque;
    public double InverseMass;
    public double AngularVelocity;
    public double InverseInertia;
    public double RestitutionCoeff;
    public byte SkipFrames;
    public byte CurrentFrame;

    readonly Double2 IRigidBody2D.Velocity => Velocity;

    readonly double IRigidBody2D.InverseMass => InverseMass;

    readonly double IRigidBody2D.RestitutionCoeff => RestitutionCoeff;

    public bool TryIntegrate()
    {
        if (CurrentFrame == SkipFrames)
        {
            CurrentFrame = 0;
            return true;
        }

        CurrentFrame++;
        return false;
    }

    // Acceleration
    //    F = mA
    // => A = F * 1/m

    // Explicit Euler
    // x += v * dt
    // v += (1/m * F) * dt

    // Semi-Implicit (Symplectic) Euler
    // v += (1/m * F) * dt
    // x += v * dt

    // see http://www.niksula.hut.fi/~hkankaan/Homepages/gravity.html
    public void IntegrateVelocity(Double2 gravity, double halfDt)
    {
        if (InverseMass == 0.0)
            return;

        Velocity += (Force * InverseMass + gravity) * halfDt;
    }

    public void IntegrateAngular(double halfDt)
    {
        if (InverseInertia == 0.0)
            return;

        AngularVelocity += Torque * InverseInertia * halfDt;
    }

    public void IntegrateVelocity(ref Transform2D transform, Double2 gravity, double halfDt)
    {
        IntegrateVelocity(gravity, halfDt);
        transform.Position += Velocity * (halfDt + halfDt);
    }

    public void IntegrateAngular(ref Transform2D transform, double halfDt)
    {
        IntegrateAngular(halfDt);
        double rotation = transform.Rotation + AngularVelocity * (halfDt + halfDt);
        transform.Rotation = MathG.NaiveFMod(rotation, Math.PI * 2);
    }

    public void ApplyForce(Double2 force)
    {
        Force += force;
    }

    public void ApplyTorque(double torque)
    {
        Torque += torque;
    }

    public void ApplyImpulse(Double2 impulse, Double2 contactVector)
    {
        Velocity += InverseMass * impulse;
        AngularVelocity += InverseInertia * Double2.Cross(contactVector, impulse);
    }
}
