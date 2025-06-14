using System;
using System.Numerics;
using System.Runtime.InteropServices;
using MonoGame.Framework;
using MonoGame.Framework.Graphics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine;

public struct Contact2D
{
    public Double2 Normal;
    public Double2 Point;
    public double Depth;
}

public struct BodyContact2D
{
    public int BodyIndex1;
    public int BodyIndex2;
    public Contact2D Contact;
}

readonly struct CircleToCircleContactGenerator : IContactGenerator<CircleBody, CircleBody>
{
    public bool Generate(ref CircleBody a, ref CircleBody b, out Contact2D contact)
    {
        contact = default;

        Circle cA = new(a.Position, a.Radius);
        Circle cB = new(b.Position, b.Radius);

        Double2 v = b.Velocity - a.Velocity;
        Double2 normal = cB.Origin - cA.Origin;

        if (Double2.Dot(v, normal) > 0)
        {
            // circles are moving apart
            return false;
        }

        if ((cA.Intersect(cB, out Double2 hitA, out Double2 hitB, out double distance) & IntersectionResult.Cuts) == 0)
        {
            return false;
        }

        contact = new Contact2D()
        {
            Normal = normal / distance,
            Point = (hitA + hitB) / 2,
            Depth = (cA.Radius + cB.Radius) - distance
        };
        return true;
    }
}

readonly struct CircleToPlaneContactGenerator : IContactGenerator<CircleBody, Plane2D>
{
    public bool Generate(ref CircleBody a, ref Plane2D plane, out Contact2D contact)
    {
        contact = default;

        Circle circle = new(a.Position, a.Radius);

        if (!plane.Intersect(circle, out Double2 hitA, out Double2 hitB, out double depth))
        {
            return false;
        }

        contact = new Contact2D()
        {
            Normal = plane.Normal,
            Point = (hitA + hitB) / 2,
            Depth = depth
        };
        return true;
    }
}

public readonly struct PlaneBody2D : ITransform2D, IRigidBody2D
{
    public readonly Plane2D Data;

    public Double2 Position { get => default; set { } }

    public Double2 Velocity => default;

    public double InverseMass => 1.0 / 1_000_000_000;

    public double RestitutionCoeff => 0;

    public void ApplyImpulse(Double2 impulse, Double2 contactVector)
    {
    }
}

public class PhysicsWorld
{
    private Storage<BodyContact2D> _circleContacts = new();
    private Storage<BodyContact2D> _planeContacts = new();

    private Storage<CircleBody> _bodies;
    public Storage<Plane2D> _planes = new();

    public PhysicsWorld(Storage<CircleBody> bodies)
    {
        _bodies = bodies;

        _planes.Add() = new Plane2D(new Double2(0, 1), 0);
    }

    public void FixedUpdate(double deltaTime)
    {
        _circleContacts.Clear();
        CircleToCircleContactGenerator gen1 = new();
        Span<CircleBody> bodies = _bodies.AsSpan();
        GenerateContacts(deltaTime, ref gen1, bodies, _circleContacts);

        _planeContacts.Clear();
        CircleToPlaneContactGenerator gen2 = new();
        Span<Plane2D> planes = _planes.AsSpan();
        GenerateContacts(deltaTime, ref gen2, bodies, planes, _planeContacts);

        double errorReduction = 0.05;

        SolveContacts(errorReduction, _circleContacts.AsSpan(), bodies, bodies);

        Span<PlaneBody2D> planeBodies = MemoryMarshal.Cast<Plane2D, PlaneBody2D>(planes);
        SolveContacts(errorReduction, _planeContacts.AsSpan(), bodies, planeBodies);
    }

    public void DrawWorld(AssetRegistry assets, SpriteBatch spriteBatch, float scale)
    {
        Span<CircleBody> bodies = _bodies.AsSpan();
        DrawContacts(spriteBatch, _circleContacts.AsSpan(), bodies, bodies);

        Span<PlaneBody2D> planeBodies = MemoryMarshal.Cast<Plane2D, PlaneBody2D>(_planes.AsSpan());
        DrawContacts(spriteBatch, _planeContacts.AsSpan(), bodies, planeBodies);

        double planeWidth = 10000;
        foreach (Plane2D plane in _planes.AsSpan())
        {
            Vector2 planeCenter = (Vector2) (plane.Normal * plane.D);
            Vector2 planeOrthog = (Vector2) (plane.Normal.RotateCW() * planeWidth);
            spriteBatch.DrawLine(planeCenter - planeOrthog, planeCenter + planeOrthog, Color.Purple, 2f);
        }
    }

    private void DrawContacts<T1, T2>(SpriteBatch spriteBatch, Span<BodyContact2D> contacts, Span<T1> span1, Span<T2> span2)
    {
        foreach (ref BodyContact2D bodyContact in contacts)
        {
            ref T1 o1 = ref span1[bodyContact.BodyIndex1];
            ref T2 o2 = ref span2[bodyContact.BodyIndex2];
            Contact2D contact = bodyContact.Contact;

            Vector2 origin = (Vector2) contact.Point;
            spriteBatch.DrawLine(origin, origin + (Vector2) (contact.Normal * contact.Depth), Color.Purple);
        }
    }

    public void SolveContacts<T1, T2>(
        double errorReduction,
        ReadOnlySpan<BodyContact2D> contacts,
        Span<T1> span1,
        Span<T2> span2)
        where T1 : ITransform2D, IRigidBody2D
        where T2 : ITransform2D, IRigidBody2D
    {
        foreach (BodyContact2D bodyContact in contacts)
        {
            ref T1 o1 = ref span1[bodyContact.BodyIndex1];
            ref T2 o2 = ref span2[bodyContact.BodyIndex2];
            Contact2D contact = bodyContact.Contact;

            double mA = 1.0 / o1.InverseMass;
            double mB = 1.0 / o2.InverseMass;
            double massRatio = (mA * mB) / (mA + mB);

            // Collision response.
            double direction = Double2.Dot(o2.Velocity - o1.Velocity, contact.Normal);
            double impulseMag = massRatio * direction;
            Double2 impulse = impulseMag * contact.Normal;

            o1.ApplyImpulse(impulse * (1 + o1.RestitutionCoeff), contact.Point);
            o2.ApplyImpulse(-impulse * (1 + o2.RestitutionCoeff), contact.Point);

            // Overlap correction.
            double pFactor = errorReduction * massRatio * contact.Depth;
            o1.Position -= pFactor * o1.InverseMass * contact.Normal;
            o2.Position += pFactor * o2.InverseMass * contact.Normal;
        }
    }

    public void GenerateContacts<TGen, T>(
        double deltaTime, ref TGen generator, Span<T> span, Storage<BodyContact2D> contacts)
        where TGen : IContactGenerator<T, T>
    {
        if (deltaTime == 0)
            return;

        for (int i = 0; i < span.Length; i++)
        {
            ref T o1 = ref span[i];

            for (int j = i + 1; j < span.Length; j++)
            {
                ref T o2 = ref span[j];

                if (generator.Generate(ref o1, ref o2, out Contact2D contact))
                {
                    contacts.Add() = new BodyContact2D()
                    {
                        BodyIndex1 = i,
                        BodyIndex2 = j,
                        Contact = contact
                    };
                }
            }
        }
    }

    public void GenerateContacts<TGen, T1, T2>(
        double deltaTime, ref TGen generator, Span<T1> span1, Span<T2> span2, Storage<BodyContact2D> contacts)
        where TGen : IContactGenerator<T1, T2>
    {
        if (deltaTime == 0)
            return;

        for (int i = 0; i < span1.Length; i++)
        {
            ref T1 o1 = ref span1[i];

            for (int j = 0; j < span2.Length; j++)
            {
                ref T2 o2 = ref span2[j];

                if (generator.Generate(ref o1, ref o2, out Contact2D contact))
                {
                    contacts.Add() = new BodyContact2D()
                    {
                        BodyIndex1 = i,
                        BodyIndex2 = j,
                        Contact = contact
                    };
                }
            }
        }
    }
}
