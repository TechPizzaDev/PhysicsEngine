using System;
using System.Numerics;
using MonoGame.Framework;
using MonoGame.Framework.Graphics;
using PhysicsEngine.Collections;
using PhysicsEngine.Collision;
using PhysicsEngine.Numerics;
using PhysicsEngine.Shapes;

namespace PhysicsEngine;

public class PhysicsWorld
{
    #region Contacts 

    private Storage<BodyContact2D> _circleContacts = new();
    private Storage<BodyContact2D> _planeContacts = new();

    private Storage<ContactFlair2D> _contactFlairs = new();

    #endregion

    private EnumMap<ShapeKind, object> _bodyStorageMap = new();
    private uint _nextBodyId = 1;

    #region Variables

    public Double2 Gravity = new(0, -9.82);

    public double ErrorReduction = 0.05;

    public bool EnableVelocity = true;
    public bool EnableAngular = true;

    public bool LineTrail;

    public float WindFlowLineOpacity = 0.5f;

    #endregion

    public PhysicsWorld()
    {
    }

    public void FixedUpdate(in UpdateState state, double deltaTime)
    {
        if (deltaTime <= 0)
            return;

        IntegrateBodies(deltaTime, state.Scale);

        Span<CircleBody> bodies = GetStorage<CircleBody>().AsSpan();

        ApplyZone(deltaTime, GetStorage<WindZone>().AsSpan(), bodies);
        ApplyZone(deltaTime, GetStorage<FluidZone>().AsSpan(), bodies);
        ApplyExplosions(deltaTime, GetStorage<ExplosionBody2D>().AsSpan(), bodies);

        _circleContacts.Clear();

        CircleToCircleContactGenerator gen1 = new(true, IntersectionResult.Cuts, CollisionMask.All);
        GenerateContacts(ref gen1, bodies, _circleContacts);

        _planeContacts.Clear();
        CircleToPlaneContactGenerator gen2 = new(CollisionMask.All);
        Span<PlaneBody2D> planes = GetStorage<PlaneBody2D>().AsSpan();
        GenerateContacts(ref gen2, bodies, planes, _planeContacts);

        SolveContacts(ErrorReduction, _circleContacts.AsSpan(), bodies, bodies);

        SolveContacts(ErrorReduction, _planeContacts.AsSpan(), bodies, planes);
    }

    #region Body storage

    public Storage<T> GetStorage<T>()
        where T : IShapeId
    {
        return (Storage<T>) _bodyStorageMap.Get(T.Kind, _ => new Storage<T>());
    }

    private BodyId MakeBodyId()
    {
        return new BodyId(_nextBodyId++);
    }

    public ref T Add<T>()
        where T : IShapeId, new()
    {
        ref T body = ref GetStorage<T>().Add();
        body = new T();
        body.Id = MakeBodyId();
        return ref body;
    }

    public ref T Add<T>(T value)
        where T : IShapeId
    {
        Storage<T> storage = GetStorage<T>();
        BodyId id = value.Id;
        if (id == default)
        {
            value.Id = MakeBodyId();
        }
        else if (!IsNewId(id, storage))
        {
            ThrowDuplicateId();
        }

        ref T body = ref storage.Add();
        body = value;
        return ref body;
    }

    private static bool IsNewId<T>(BodyId id, Storage<T> storage)
        where T : IShapeId
    {
        var span = storage.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Id == id)
            {
                return false;
            }
        }
        return true;
    }

    private static void ThrowDuplicateId() => throw new InvalidOperationException();

    #endregion

    #region Force integration

    private void IntegrateBody(double halfDt, Double2 gravity, ref Transform2D transform, ref RigidBody2D body)
    {
        if (body.TryIntegrate())
        {
            double dt = halfDt * (body.SkipFrames + 1);

            if (EnableVelocity)
            {
                body.IntegrateVelocity(ref transform, gravity, dt);
                body.IntegrateVelocity(gravity, dt);
            }

            if (EnableAngular)
            {
                body.IntegrateAngular(dt);
                body.IntegrateAngular(ref transform, dt);
            }
        }

        body.Force = default;
        body.Torque = 0;
    }

    private void IntegrateBodies(double deltaTime, float trailScale)
    {
        double halfDt = deltaTime * 0.5;

        Double2 gravity = Gravity;

        foreach (ref CircleBody circle in GetStorage<CircleBody>().AsSpan())
        {
            IntegrateBody(halfDt, gravity, ref circle.Transform, ref circle.RigidBody);

            if (LineTrail)
            {
                circle.trail?.Update(
                    (Vector2) circle.Transform.Position,
                    (Vector2) circle.RigidBody.Velocity,
                    trailScale);
            }
        }
    }

    #endregion

    #region Apply Zones

    private void ApplyZone<TZone, TBody>(double deltaTime, Span<TZone> zones, Span<TBody> bodies)
        where TZone : IZone2D
        where TBody : IRigidBody2D, IShape2D
    {
        Double2 gravity = Gravity;

        foreach (ref TZone zone in zones)
        {
            zone.Update(deltaTime);

            Bound2 zoneBounds = zone.GetBounds();

            foreach (ref TBody body in bodies)
            {
                Bound2 intersection = zoneBounds.Intersect(body.GetBounds());
                if (!intersection.HasArea())
                    continue;

                // TODO: use more accurate intersection?
                zone.Apply(ref body, intersection, gravity);
            }
        }
    }

    private static void ApplyExplosions<T>(double deltaTime, Span<ExplosionBody2D> explosions, Span<T> bodies)
        where T : IRigidBody2D, ITransform2D
    {
        foreach (ref ExplosionBody2D explosion in explosions)
        {
            explosion.Update(deltaTime);

            if (!explosion.ShouldApply)
                continue;

            foreach (ref T body in bodies)
                explosion.Apply(ref body);
        }
    }

    #endregion

    #region Drawing

    private static void DrawPlane(SpriteBatch spriteBatch, double planeWidth, Plane2D plane, Color color, float thickness)
    {
        Vector2 planeCenter = (Vector2) (plane.Normal * plane.D);
        Vector2 planeOrthog = (Vector2) (plane.Normal.RotateCW() * planeWidth);
        spriteBatch.DrawLine(planeCenter - planeOrthog, planeCenter + planeOrthog, color, thickness);
    }

    private void DrawWindZone(
        SpriteBatch spriteBatch, Vector2 lineSpacing, RectangleF viewport, float lineWidth, in WindZone zone)
    {
        RectangleF fullRect = zone.Bounds.ToRectF();
        RectangleF rect = RectangleF.Intersection(fullRect, viewport);
        if (rect.IsEmpty)
        {
            return;
        }

        spriteBatch.FillRectangle(fullRect, new Color(Color.Lime, 24));
        spriteBatch.DrawRectangle(fullRect, new Color(Color.LimeGreen, 200));

        if (WindFlowLineOpacity <= 0f)
        {
            return;
        }

        int cols = (int) float.Floor(rect.Width / lineSpacing.X);
        int rows = (int) float.Floor(rect.Height / lineSpacing.Y);
        Vector2 origin = rect.Position / lineSpacing;
        origin.X = float.Ceiling(origin.X);
        origin.Y = float.Ceiling(origin.Y);
        origin = origin * lineSpacing + lineSpacing / 2;

        var color = QuadCorner<Color>.Vertical(new(0, 1f, 0, WindFlowLineOpacity), new(0, 255, 0, 0));

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Vector2 p0 = origin + new Vector2(x, y) * lineSpacing;
                (double angle, double strength) = zone.EvaluateTurbulence(p0);

                Double2 dir = zone.Direction.Rotate(Double2.SinCos(angle));
                Vector2 p1 = p0 + (Vector2) (dir * lineSpacing * strength);

                spriteBatch.DrawLine(p0, p1, color, lineWidth);
            }
        }
    }

    private static void DrawFluidZone(SpriteBatch spriteBatch, in FluidZone zone)
    {
        RectangleF rect = zone.Bounds.ToRectF();
        spriteBatch.FillRectangle(rect, new Color(Color.Blue, 24));
        spriteBatch.DrawRectangle(rect, new Color(Color.DeepSkyBlue, 200));
    }

    private static void DrawExplosionBody(SpriteBatch spriteBatch, in ExplosionBody2D explosion)
    {
        Vector2 center = (Vector2) explosion.Position;
        float radius = (float) explosion.Radius;
        int sides = 100;

        int progress = (int) Math.Clamp((explosion.Time / explosion.Interval) * sides, 0, sides);

        spriteBatch.DrawSlicedCircle(center, radius, sides, sides - progress, Color.Yellow, 2, clockwise: false);
        spriteBatch.DrawSlicedCircle(center, radius + 1, sides, progress, Color.Red, 4, clockwise: true);
    }

    public void DrawWorld(in DrawState state)
    {
        SpriteBatch spriteBatch = state.SpriteBatch;

        float inv_scale = 1f / state.Scale;

        float planeThick = inv_scale;
        double planeWidth = 10000;
        foreach (ref PlaneBody2D plane in GetStorage<PlaneBody2D>().AsSpan())
        {
            DrawPlane(spriteBatch, planeWidth, plane.Data, plane.Color, planeThick);
        }

        float lineWidth = 2f * inv_scale;
        Vector2 lineSpacing = new(50 / (Math.Min(2f, state.Scale)));
        foreach (ref WindZone zone in GetStorage<WindZone>().AsSpan())
        {
            DrawWindZone(spriteBatch, lineSpacing, state.WorldViewport, lineWidth, zone);
        }

        foreach (ref FluidZone zone in GetStorage<FluidZone>().AsSpan())
        {
            DrawFluidZone(spriteBatch, zone);
        }

        foreach (ref ExplosionBody2D explosion in GetStorage<ExplosionBody2D>().AsSpan())
        {
            DrawExplosionBody(spriteBatch, explosion);
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

            DrawFlair(spriteBatch, flairLife, flair, inv_scale);
        }
    }

    private static void DrawFlair(SpriteBatch spriteBatch, int flairLife, ContactFlair2D flair, float scale)
    {
        float progress = flair.FrameCount / (float) flairLife;
        float size = (1f - progress) * 5f + 5f;

        Color pointColor = new(Color.MediumPurple, 0.5f - progress * 0.4f);

        spriteBatch.DrawPoint(flair.Point, pointColor, size * scale, 0f, MathF.PI / 4);
        // TODO: draw vector?
        //spriteBatch.DrawLine(flair.Point, flair.Point + flair.Direction, Color.Purple);
    }

    #endregion

    #region Contact solver

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

        double massRatio;
        if (double.IsInfinity(mA))
        {
            massRatio = mB;
        }
        else if (double.IsInfinity(mB))
        {
            massRatio = mA;
        }
        else
        {
            massRatio = (mA * mB) / (mA + mB);
        }

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

    #endregion

    #region GetObjectsInRange

    public void GetObjectsInRange(Double2 position, float radius, Storage<ShapeLocation> output)
    {
        CircleBody origin = new()
        {
            Position = position,
            Radius = radius
        };

        void Get<G, T>(G generator)
            where G : IContactGenerator<CircleBody, T>
            where T : IShapeId
        {
            GetContacts<G, CircleBody, T>(output, ref origin, generator, GetStorage<T>().AsSpan());
        }

        Get<CircleToCircleContactGenerator, CircleBody>(new(false, IntersectionResult.Any, CollisionMask.None));
        Get<CircleToPlaneContactGenerator, PlaneBody2D>(new(CollisionMask.None));
        Get<CircleToExplosionContactGenerator, ExplosionBody2D>(new(IntersectionResult.Any));
        Get<CircleToShapeContactGenerator<WindZone>, WindZone>(new());
        Get<CircleToShapeContactGenerator<FluidZone>, FluidZone>(new());
    }

    private static void GetContacts<G, T1, T2>(
        Storage<ShapeLocation> output, ref T1 origin, G generator, Span<T2> bodies)
        where G : IContactGenerator<T1, T2>
        where T2 : IShapeId
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            ref T2 body = ref bodies[i];
            if (generator.Generate(ref origin, ref body, out Contact2D contact))
            {
                output.Add() = new ShapeLocation(T2.Kind, body.Id, contact);
            }
        }
    }

    #endregion
}
