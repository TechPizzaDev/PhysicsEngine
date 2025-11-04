using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using MonoGame.Framework;
using MonoGame.Framework.Graphics;
using PhysicsEngine.Collections;
using PhysicsEngine.Numerics;

namespace PhysicsEngine.Drawing;

public class Trail
{
    private Ring<Vector2> _points;

    private Vector2 _lastVelocity;
    private Vector2 _lastDelta;
    private Vector2 _lastPoint;
    private Vector2 _accumulator;
    private int _accCount = -1;

    public int Capacity => _points.Capacity;

    public float FadeFactor { get; set; } = 1f;

    public Trail(int capacity)
    {
        _points = new Ring<Vector2>(capacity);
    }

    public void Update(Vector2 point, Vector2 velocity, float rangeScale)
    {
        float dirChange = Vector2.Dot(_lastVelocity, velocity);
        _lastVelocity = velocity;

        Vector2 lastPoint = _lastPoint;

        Vector2 delta = point - _lastPoint;
        _lastDelta = delta;
        _lastPoint = point;

        _accumulator += delta;

        if (dirChange < 0)
        {
            _points.PushFront(lastPoint);
        }

        int countThresh = 8;
        float rangeThresh = 20 / rangeScale;

        if ((uint) _accCount < (uint) countThresh &&
            _accumulator.LengthSquared() < rangeThresh * rangeThresh)
        {
            _accCount++;
            return;
        }

        _accCount = default;
        _accumulator = default;

        _points.PushFront(point);
    }

    public void Push(Vector2 point)
    {
        _lastDelta = point - _lastPoint;
        _lastPoint = point;
        _points.PushFront(point);
    }

    public void DrawLines<C>(SpriteBatch spriteBatch, C color, float width, float depth = 0f)
        where C : IQuad<Color>
    {
        DrawLines(spriteBatch, _lastPoint, _lastDelta, _points, color, FadeFactor, width, depth);
    }

    public static void DrawLines<C>(
        SpriteBatch spriteBatch, Vector2 origin, Vector2 direction, Ring<Vector2> points,
        C color, float fade, float width, float depth = 0f)
        where C : IQuad<Color>
    {
        Texture2D texture = SpriteBatchShapeExtensions.GetWhitePixelTexture(spriteBatch.GraphicsDevice);

        Vector3 orthoL = DeltaToOrthogonalDir(direction, width);
        Vector2 pointL = origin;

        float inv_count = fade * (1f / points.Capacity);
        float ageL = 1f;

        for (int j = 0; j < 2; j++)
        {
            Span<Vector2> span = j == 0 ? points.GetHeadSpan() : points.GetTailSpan();

            for (int i = 0; i < span.Length; i++)
            {
                Vector2 pointR = span[i];
                Vector2 delta = pointL - pointR;
                if (delta == Vector2.Zero)
                {
                    continue;
                }
                // Get quad early to avoid register spills on rare allocs.
                ref SpriteQuad quad = ref spriteBatch.GetBatchQuad(texture, depth);

                float ageR = ageL - inv_count;
                float widthR = (ageR + 0.1f) / 1.1f * width;
                Vector3 orthoR = DeltaToOrthogonalDir(delta, widthR);

                // Try to correct sharp turns.
                if (Vector3.Dot(orthoR, orthoL) < 0f)
                {
                    orthoL = orthoR;
                    pointL -= RotateCW(orthoL.AsVector128()).AsVector2() * 0.75f;

                    //spriteBatch.DrawPoint(pointL, Color.LimeGreen, width * 0.5f);
                    //spriteBatch.DrawPoint(pointR, Color.Red, width * 0.5f);
                }

                uint alphaL = (uint) float.ConvertToIntegerNative<int>(ageL * 255f);
                uint alphaR = (uint) float.ConvertToIntegerNative<int>(ageR * 255f);
                ageL = ageR;

                // Cheap alpha interpolation for all corners in bulk.
                Vector128<ushort> alpha = Vector128.Narrow(Vector128.Create(alphaL), Vector128.Create(alphaR));
                Vector128<byte> color4x = color.ToVector128().AsByte();
                Vector128<uint> colors = Composition.ApplyAlpha(color4x, alpha).AsUInt32();

                // Create corners of quad by adding rotated thickness to points.
                Vector3 pL = Vector3.Create(pointL, depth);
                Vector3 pTL = pL + orthoL;
                Vector3 pBL = pL - orthoL;

                Vector3 pR = Vector3.Create(pointR, depth);
                Vector3 pTR = pR + orthoR;
                Vector3 pBR = pR - orthoR;

                pointL = pointR;
                orthoL = orthoR;

                quad.VertexTL.SetPositionAndColor(pTL, colors, 0);
                quad.VertexBL.SetPositionAndColor(pBL, colors, 2);
                quad.VertexTR.SetPositionAndColor(pTR, colors, 1);
                quad.VertexBR.SetPositionAndColor(pBR, colors, 3);
            }
        }
        spriteBatch.FlushIfNeeded();
    }

    /// <summary>
    /// Given the direction (delta), rotate it by 90 degrees.
    /// This is effectively a cheap plane rotation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 DeltaToOrthogonalDir(Vector2 delta, float width)
    {
        Vector128<float> norm = Vector2.Normalize(delta).AsVector128Unsafe();
        Vector128<float> flip = RotateCW(norm);
        return flip.AsVector3() * width;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> RotateCW(Vector128<float> x)
    {
        Vector128<float> transpose = Vector128.Shuffle(x, Vector128.Create(1, 0, 3, 2));
        return transpose ^ Vector128.Create(-0.0f, 0, 0, 0);
    }
}
