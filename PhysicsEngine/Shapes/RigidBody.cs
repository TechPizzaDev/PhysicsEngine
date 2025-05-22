namespace PhysicsEngine.Shapes;

public struct RigidBody
{
    public Double2 Velocity;
    public Double2 Force;
    public double Torque;
    public double InverseMass;
    public double AngularVelocity;
    public double InverseInertia;
    public double RestitutionCoeff;

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
    public void IntegrateForces(Double2 gravity, double dt)
    {
        if (InverseMass == 0.0)
            return;
         
        double halfDt = dt * 0.5;
        Velocity += (Force * InverseMass + gravity) * halfDt;
        AngularVelocity += Torque * InverseInertia * halfDt;
    }

    public void IntegrateVelocity(ref Transform transform, Double2 gravity, double dt)
    {
        if (InverseMass == 0.0)
            return;

        transform.Position += Velocity * dt;
        transform.Rotation += AngularVelocity * dt;
        IntegrateForces(gravity, dt);
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
