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

    readonly Double2 IRigidBody2D.Velocity => Velocity;

    readonly double IRigidBody2D.InverseMass => InverseMass;

    readonly double IRigidBody2D.RestitutionCoeff => RestitutionCoeff;

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
    public void IntegrateVelocity(Double2 gravity, double dt)
    {
        if (InverseMass == 0.0)
            return;

        double halfDt = dt * 0.5;
        Velocity += (Force * InverseMass + gravity) * halfDt;
    }

    public void IntegrateAngular(double dt)
    {
        if (InverseInertia == 0.0)
            return;

        double halfDt = dt * 0.5;
        AngularVelocity += Torque * InverseInertia * halfDt;
    }

    public void IntegrateVelocity(ref Transform2D transform, Double2 gravity, double dt)
    {
        IntegrateVelocity(gravity, dt);
        transform.Position += Velocity * dt;
    }

    public void IntegrateAngular(ref Transform2D transform, double dt)
    {
        IntegrateAngular(dt);
        transform.Rotation += AngularVelocity * dt;
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
