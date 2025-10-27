using System;
using System.Numerics;
using MonoGame.Framework;
using MonoGame.Framework.Graphics;
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

    #region Bodies

    public Storage<CircleBody> Circles = new();
    public Storage<PlaneBody2D> _planes = new();
    public Storage<ExplosionBody2D> _explosions = new();

    #endregion

    #region Zones

    public Storage<WindZone> _windZones = new();
    public Storage<FluidZone> _fluidZones = new();

    #endregion

    #region Variables

    public Double2 Gravity = new(0, -9.82);

    public double ErrorReduction = 0.05;

    public float WindFlowLineOpacity = 0.5f;

    #endregion

    public PhysicsWorld()
    {
        _planes.Add() = new PlaneBody2D(new BodyId(1))
        {
            Data = new Plane2D(new Double2(0, -1), 0)
        };

        _windZones.Add() = new WindZone(new BodyId(1))
        {
            Bounds = new Bound2(new RectangleF(0, 0, 10000, 500)),
            Speed = 100f,
            Direction = new Double2(0, 1f),
            Drag = 1.05,
            Density = 1.22,
            TurbulenceAngle = Math.PI * 0.5,
            TurbulenceIntensity = 1f,
            TurbulenceScale = new Double2(0.001),
            TurbulenceDepth = 0.1,
        };

        _windZones.Add() = new WindZone(new BodyId(2))
        {
            Bounds = new Bound2(new RectangleF(500, 0, 10000, 5000)),
            Speed = 100f,
            Direction = new Double2(0, 1f),
            Drag = 1.05,
            Density = 1.22,
            TurbulenceAngle = Math.PI * 0.5,
            TurbulenceIntensity = 1f,
            TurbulenceScale = new Double2(0.001),
            TurbulenceDepth = 0.1,
            TurbulenceSeed = 1234,
        };

        _fluidZones.Add() = new FluidZone(new BodyId(1))
        {
            Bounds = new Bound2(new RectangleF(-10000, 0, 10000, 500)),
            Density = 997,
        };

        _explosions.Add() = new ExplosionBody2D(new BodyId(1))
        {
            Transform = new() { Position = new Double2(0, 3000) },
            Radius = 4000,
            Force = 100_000_000_000,
            Interval = 3,
        };
    }

    public void FixedUpdate(double deltaTime)
    {
        if (deltaTime == 0)
            return;

        Span<CircleBody> bodies = Circles.AsSpan();

        ApplyZone(deltaTime, _windZones.AsSpan(), bodies);
        ApplyZone(deltaTime, _fluidZones.AsSpan(), bodies);
        ApplyExplosions(deltaTime, _explosions.AsSpan(), bodies);

        _circleContacts.Clear();

        CircleToCircleContactGenerator gen1 = new(true, IntersectionResult.Cuts);
        GenerateContacts(ref gen1, bodies, _circleContacts);

        _planeContacts.Clear();
        CircleToPlaneContactGenerator gen2 = new();
        Span<PlaneBody2D> planes = _planes.AsSpan();
        GenerateContacts(ref gen2, bodies, planes, _planeContacts);

        SolveContacts(ErrorReduction, _circleContacts.AsSpan(), bodies, bodies);

        SolveContacts(ErrorReduction, _planeContacts.AsSpan(), bodies, planes);
    }

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

    private static void DrawPlane(SpriteBatch spriteBatch, double planeWidth, Plane2D plane)
    {
        Vector2 planeCenter = (Vector2) (plane.Normal * plane.D);
        Vector2 planeOrthog = (Vector2) (plane.Normal.RotateCW() * planeWidth);
        spriteBatch.DrawLine(planeCenter - planeOrthog, planeCenter + planeOrthog, new Color(Color.Purple, 200), 2f);
    }

    private void DrawWindZone(
        SpriteBatch spriteBatch, Vector2 lineSpacing, RectangleF viewport, float scale, in WindZone zone)
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

                spriteBatch.DrawLine(p0, p1, color, 1f / scale);
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

        DrawCircle(spriteBatch, center, radius, sides, sides - progress, Color.Yellow, 2);
        DrawCircle(spriteBatch, center, radius - 3, sides, progress, Color.Red, 4, false);
    }

    private static void DrawCircle(
        SpriteBatch spriteBatch, Vector2 center, float radius, int sides, int count, Color color,
        float thickness = 1f,
        bool clockwise = true,
        float start = -MathF.PI / 2,
        float layerDepth = 0)
    {
        var points = new EllipseEnumerable(new Vector2(radius), sides, count + 1, start, clockwise);
        bool connect = count - 1 == sides;
        spriteBatch.DrawPolygon(center, points, color, thickness, layerDepth, connect);
    }

    public void DrawWorld(AssetRegistry assets, SpriteBatch spriteBatch, float scale)
    {
        Matrix4x4.Invert(spriteBatch.SpriteEffect.GetFinalMatrix(), out Matrix4x4 invProj);
        Vector2 viewMin = Vector2.Transform(new Vector2(-1, 1), invProj);
        Vector2 viewMax = Vector2.Transform(new Vector2(1, -1), invProj);
        RectangleF viewport = RectangleF.FromPoints(viewMin, viewMax);

        double planeWidth = 10000;
        foreach (ref PlaneBody2D plane in _planes.AsSpan())
        {
            DrawPlane(spriteBatch, planeWidth, plane.Data);
        }

        Vector2 lineSpacing = new(50 / (scale * 2));
        foreach (ref WindZone zone in _windZones.AsSpan())
        {
            DrawWindZone(spriteBatch, lineSpacing, viewport, scale, zone);
        }

        foreach (ref FluidZone zone in _fluidZones.AsSpan())
        {
            DrawFluidZone(spriteBatch, zone);
        }

        foreach (ref ExplosionBody2D explosion in _explosions.AsSpan())
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

    #endregion

    public void GetObjectsInRange(Double2 position, float radius, Storage<ShapeLocation> output)
    {
        CircleBody origin = new()
        {
            Position = position,
            Radius = radius
        };

        GetContacts(output, ref origin, new CircleToCircleContactGenerator(false, IntersectionResult.Any), Circles.AsSpan(), ShapeKind.Circle);
        GetContacts(output, ref origin, new CircleToPlaneContactGenerator(), _planes.AsSpan(), ShapeKind.Plane);
        GetContacts(output, ref origin, new CircleToExplosionContactGenerator(IntersectionResult.Any), _explosions.AsSpan(), ShapeKind.Explosion);

        GetContacts(output, ref origin, new CircleToShapeContactGenerator<WindZone>(), _windZones.AsSpan(), ShapeKind.WindZone);
        GetContacts(output, ref origin, new CircleToShapeContactGenerator<FluidZone>(), _fluidZones.AsSpan(), ShapeKind.FluidZone);
    }

    private static void GetContacts<G, T1, T2>(
        Storage<ShapeLocation> output, ref T1 origin, G generator, Span<T2> bodies, ShapeKind kind)
        where G : IContactGenerator<T1, T2>
        where T2 : IShapeId
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            ref T2 body = ref bodies[i];
            if (generator.Generate(ref origin, ref body, out Contact2D contact))
            {
                output.Add() = new ShapeLocation(kind, body.Id, contact);
            }
        }
    }
}
