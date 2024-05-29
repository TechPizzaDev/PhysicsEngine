using System.Collections.Generic;
using System.Numerics;
using MonoGame.Framework;
using MonoGame.Framework.Graphics;

namespace PhysicsEngine;

public class Trail
{
    public Queue<Vector2> Points = new();
    public int Capacity;

    public Trail(int capacity)
    {
        Capacity = capacity;
        Points = new Queue<Vector2>(Capacity);
    }

    public void Update(Vector2 point)
    {
        if (Points.Count >= Capacity)
        {
            Points.Dequeue();
        }

        Points.Enqueue(point);
    }

    public void Draw(SpriteBatch spriteBatch, Color color, float thickness)
    {
        Color startColor = Color.Lerp(color, Color.Black, 1f);

        int count = 0;
        Vector2 prevPoint = default;
        foreach (Vector2 point in Points)
        {
            if (count != 0)
            {
                float age = count / (float) Capacity;
                spriteBatch.DrawLine(prevPoint, point, Color.Lerp(startColor, color, age), age * thickness + 1);
            }

            prevPoint = point;
            count++;
        }
    }
}
