using System;

internal struct Bounds2
{
    public Vector2 Position;
    public Vector2 Size;

    public Vector2 Min => Position;
    public Vector2 Max => Position + Size;

    public Vector2 HalfSize
    {
        get => Size / 2;
        set => Size = value * 2;
    }

    public Vector2 Center
    {
        get => Position + HalfSize;
        set => Position = value - HalfSize;
    }

    public static readonly Bounds2 Zero = new Bounds2(Vector2.Zero, Vector2.Zero);

    /// <summary>
    ///     Creates a new 2D bounds rectangle.
    /// </summary>
    /// <param name="position">The origin of the bounds.</param>
    /// <param name="size">The Size of the bounds.</param>
    public Bounds2(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    /// <summary>
    ///     Creates a new 2D bounds rectangle.
    /// </summary>
    /// <param name="x">The X component of the origin of the bounds.</param>
    /// <param name="y">The Y component of the origin of the bounds.</param>
    /// <param name="width">The width of the bounds.</param>
    /// <param name="height">The height of the bounds.</param>
    public Bounds2(float x, float y, float width, float height)
    {
        Position = new Vector2(x, y);
        Size = new Vector2(width, height);
    }

    public override string ToString()
    {
        return string.Format("({0}, {1})", Position, Size);
    }

    /// <summary>
    ///     Returns true if a point is within these bounds.
    /// </summary>
    /// <param name="point">The point to test.</param>
    public bool Contains(Vector2 point)
    {
        return Min.X <= point.X && point.X <= Max.X && Min.Y <= point.Y && point.Y <= Max.Y;
    }

    public (Vector2, float) Overlap(Bounds2 box2)
    {
        // Calculate overlap in x-axis
        var overlapX = Math.Min(Max.X, box2.Max.X) - Math.Max(Min.X, box2.Min.X);
        if (overlapX <= 0) return (Vector2.Zero, 0);

        // Calculate overlap in y-axis
        var overlapY = Math.Min(Max.Y, box2.Max.Y) - Math.Max(Min.Y, box2.Min.Y);
        if (overlapY <= 0) return (Vector2.Zero, 0);

        // Determine the smallest overlap
        if (overlapX < overlapY)
        {
            // The normal will point towards the second box from the first
            var normal = Max.X < box2.Min.X ? Vector2.Left : Vector2.Right;
            return (normal, overlapX);
        }
        else
        {
            var normal = Max.Y < box2.Min.Y ? Vector2.Down : Vector2.Up;
            return (normal, overlapY);
        }
    }

    /// <summary>
    ///     Returns true if another bounds rectangle overlaps these bounds.
    /// </summary>
    /// <param name="bounds">The bounds to test.</param>
    public bool Overlaps(Bounds2 bounds)
    {
        var mDiff = MinkowskiDifference(bounds);
        var min = mDiff.Min;
        var max = mDiff.Max;

        return min[0] <= 0 && max[0] >= 0 && min[1] <= 0 && max[1] >= 0;
    }

    public Bounds2 MinkowskiDifference(Bounds2 other)
    {
        return new Bounds2
        {
            HalfSize = HalfSize + other.HalfSize,
            Center = Center - other.Center
        };
    }

    public Vector2 PenetrationVector()
    {
        var min = Min;
        var max = Max;

        var minDist = Math.Abs(min[0]);
        var outVec = new Vector2(min[0], 0);

        if (Math.Abs(max[0]) < minDist)
        {
            minDist = Math.Abs(max[0]);
            outVec[0] = max[0];
        }

        if (Math.Abs(min[1]) < minDist)
        {
            minDist = Math.Abs(min[1]);
            outVec = new Vector2(0, min[1]);
        }

        if (Math.Abs(max[1]) < minDist)
        {
            outVec = new Vector2(0, max[1]);
        }

        return outVec;
    }

    public float Top => Position.Y;

    public float Left => Position.X;

    public float Right => Position.X + Size.X;

    public float Bottom => Position.Y + Size.Y;
}