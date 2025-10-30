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

    private Vector2 _lastDelta;
    private Vector2 _lastPoint;
    private Vector2 _accumulator;
    private int _accCount = -1;

    public Trail(int capacity)
    {
        _points = new Ring<Vector2>(capacity);
    }

    public void Update(Vector2 point)
    {
        Vector2 delta = point - _lastPoint;
        _lastDelta = delta;
        _lastPoint = point;

        _accumulator += delta;

        int countThresh = 8;
        int rangeThresh = 4;

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

    public void DrawLines<C>(SpriteBatch spriteBatch, C color, float width, float depth = 0f)
        where C : IQuad<Color>
    {
        Texture2D texture = SpriteBatchShapeExtensions.GetWhitePixelTexture(spriteBatch.GraphicsDevice);

        Vector3 orthoL = DeltaToOrthogonalDir(_lastDelta, width);
        Vector2 pointL = _lastPoint;

        float inv_count = 1f / _points.Capacity;
        float ageL = 1f;

        for (int j = 0; j < 2; j++)
        {
            Span<Vector2> points = j == 0 ? _points.GetHeadSpan() : _points.GetTailSpan();

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 pointR = points[i];
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
    private static Vector3 DeltaToOrthogonalDir(Vector2 delta, float width)
    {
        Vector128<float> norm = Vector2.Normalize(delta).AsVector128Unsafe();
        Vector128<float> transpose = Vector128.Shuffle(norm, Vector128.Create(1, 0, 3, 2));
        Vector128<float> flip = transpose ^ Vector128.Create(-0.0f, 0, 0, 0);
        return flip.AsVector3() * width;
    }
}
