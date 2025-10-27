using System.Collections.Generic;
using System.Numerics;
using MonoGame.Framework;
using MonoGame.Framework.Graphics;

namespace PhysicsEngine.Drawing;

public class Trail
{
    public Queue<Vector2> Points = new();
    public int Capacity;

    private Vector2 lastPoint;
    private Vector2 accumulator;
    private int accCount = 4;

    public Trail(int capacity)
    {
        Capacity = capacity;
        Points = new Queue<Vector2>(Capacity);
    }

    public void Update(Vector2 point)
    {
        Vector2 diff = point - lastPoint;
        lastPoint = point;

        accumulator += diff;

        int countThresh = 8;
        int rangeThresh = 4;

        if (accCount < countThresh &&
            accumulator.LengthSquared() < rangeThresh * rangeThresh)
        {
            accCount++;
            return;
        }
        
        accCount = default;
        accumulator = default;

        if (Points.Count >= Capacity)
        {
            Points.Dequeue();
        }

        Points.Enqueue(point);
    }

    public void Draw(SpriteBatch spriteBatch, Color color, float thickness)
    {
        Color startColor = Color.Lerp(color, Color.Black, 1f);

        int start = Capacity - Points.Count;
        int count = start;
        Vector2 prevPoint = default;
        Vector2 dir = default;

        foreach (Vector2 point in Points)
        {
            if (count != start)
            {
                dir = point - prevPoint;
                DrawLine(spriteBatch, prevPoint, point, dir, count, startColor, color, thickness);
            }

            prevPoint = point;
            count++;
        }

        DrawLine(spriteBatch, prevPoint, lastPoint, dir, count, startColor, color, thickness);
    }

    private void DrawLine(
        SpriteBatch spriteBatch, Vector2 prevPoint, Vector2 point, Vector2 dir,
        int count, Color startColor, Color color, float thickness)
    {
        float age = count / (float) Capacity;
        float thick = (age + 0.1f) / 1.1f * thickness;
        spriteBatch.DrawLine(prevPoint - dir, point, Color.Lerp(startColor, color, age), thick);
    }
}
