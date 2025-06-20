using System;
using System.Numerics;
using System.Runtime.InteropServices;
using MonoGame.Framework;
using MonoGame.Framework.Graphics;
using PhysicsEngine.Collision;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine;

public class PhysicsWorld
{
    private Storage<BodyContact2D> _circleContacts = new();
    private Storage<BodyContact2D> _planeContacts = new();

    private Storage<ContactFlair2D> _contactFlairs = new();

    private Storage<CircleBody> _bodies;

    public Storage<Plane2D> _planes = new();

    public Double2 Gravity = new(0, -9.82);

    public PhysicsWorld(Storage<CircleBody> bodies)
    {
        _bodies = bodies;

        _planes.Add() = new Plane2D(new Double2(0, -1), 0);
    }

    public void FixedUpdate(double deltaTime)
    {
        if (deltaTime == 0)
            return;

        Span<CircleBody> bodies = _bodies.AsSpan();
        _circleContacts.Clear();

        CircleToCircleContactGenerator gen1 = new();
        GenerateContacts(ref gen1, bodies, _circleContacts);

        _planeContacts.Clear();
        CircleToPlaneContactGenerator gen2 = new();
        Span<Plane2D> planes = _planes.AsSpan();
        GenerateContacts(ref gen2, bodies, planes, _planeContacts);

        double errorReduction = 0.05;

        SolveContacts(errorReduction, _circleContacts.AsSpan(), bodies, bodies);

        Span<PlaneBody2D> planeBodies = MemoryMarshal.Cast<Plane2D, PlaneBody2D>(planes);
        SolveContacts(errorReduction, _planeContacts.AsSpan(), bodies, planeBodies);
    }

    private static void DrawPlane(SpriteBatch spriteBatch, double planeWidth, Plane2D plane)
    {
        Vector2 planeCenter = (Vector2) (plane.Normal * plane.D);
        Vector2 planeOrthog = (Vector2) (plane.Normal.RotateCW() * planeWidth);
        spriteBatch.DrawLine(planeCenter - planeOrthog, planeCenter + planeOrthog, new Color(Color.Purple, 200), 2f);
    }

    private static void DrawWindZone(SpriteBatch spriteBatch, in WindZone zone)
    {
        RectangleF rect = zone.Bounds.ToRectF();
        spriteBatch.FillRectangle(rect, new Color(Color.Lime, 24));
        spriteBatch.DrawRectangle(rect, new Color(Color.LimeGreen, 200));
    }

    private static void DrawFluidZone(SpriteBatch spriteBatch, in FluidZone zone)
    {
        RectangleF rect = zone.Bounds.ToRectF();
        spriteBatch.FillRectangle(rect, new Color(Color.Blue, 24));
        spriteBatch.DrawRectangle(rect, new Color(Color.DeepSkyBlue, 200));
    }

    public void DrawWorld(AssetRegistry assets, SpriteBatch spriteBatch, float scale)
    {
        double planeWidth = 10000;
        foreach (Plane2D plane in _planes.AsSpan())
        {
            DrawPlane(spriteBatch, planeWidth, plane);
        }

        int flairLife = 20;

        Span<ContactFlair2D> flairs = _contactFlairs.AsSpan();
        for (int i = 0; i < flairs.Length; i++)
        {
            ref ContactFlair2D flair = ref flairs[i];
            if (flair.FrameCount >= flairLife)
            {
                _contactFlairs.RemoveAt(i);
                flairs = flairs[..^1];
                continue;
            }
            flair.FrameCount++;

            DrawFlair(spriteBatch, flairLife, flair);
        }
    }

    private static void DrawFlair(SpriteBatch spriteBatch, int flairLife, ContactFlair2D flair)
    {
        float progress = flair.FrameCount / (float) flairLife;
        float size = (1f - progress) * 5f + 5f;

        Color pointColor = new(Color.MediumPurple, 0.5f - progress * 0.4f);

        spriteBatch.DrawPoint(flair.Point, pointColor, size, 0f, MathF.PI / 4);
        // TODO: draw vector?
        //spriteBatch.DrawLine(flair.Point, flair.Point + flair.Direction, Color.Purple);
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

            SolveContact(errorReduction, contact, ref o1, ref o2);

            // Store contacts as flair.
            _contactFlairs.Add() = new ContactFlair2D()
            {
                Point = (Vector2) contact.Point,
                Direction = (Vector2) (contact.Normal * contact.Depth),
            };
        }
    }

    private static void SolveContact<T1, T2>(double errorReduction, Contact2D contact, ref T1 o1, ref T2 o2)
        where T1 : ITransform2D, IRigidBody2D
        where T2 : ITransform2D, IRigidBody2D
    {
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

    public static void GenerateContacts<TGen, T>(
        ref TGen generator, Span<T> span, Storage<BodyContact2D> contacts)
        where TGen : IContactGenerator<T, T>
    {
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

    public static void GenerateContacts<TGen, T1, T2>(
        ref TGen generator, Span<T1> span1, Span<T2> span2, Storage<BodyContact2D> contacts)
        where TGen : IContactGenerator<T1, T2>
    {
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
