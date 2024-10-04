using System;
using System.ComponentModel;

internal enum Direction
{
    Left,
    Right,
    Up,
    Down,
    None
}

internal class MovingBounds
{
    [DefaultValue(typeof(Vector2), "Vector2.Zero")]
    public Vector2 Left = Vector2.Zero;

    [DefaultValue(typeof(Vector2), "Vector2.Zero")]
    public Vector2 Right = Vector2.Zero;

    public static MovingBounds Default => new MovingBounds
    {
        Left = Vector2.Zero,
        Right = Vector2.Zero
    };

    public Vector2 this[int index]
    {
        get
        {
            if (index < 0 || index > 1) throw new ArgumentOutOfRangeException();

            if (index == 0) return Left;
            return Right;
        }

        set
        {
            if (index < 0 || index > 1) throw new ArgumentOutOfRangeException();

            if (index == 0) Left = value;
            else Right = value;
        }
    }

    public override string ToString()
    {
        return $"(Left: {Left}, Right: {Right})";
    }

    public override bool Equals(object obj)
    {
        if (!(obj is MovingBounds b)) return false;

        return b.Left == Left && b.Right == Right;
    }

    public static bool operator ==(MovingBounds a, MovingBounds b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(MovingBounds a, MovingBounds b)
    {
        return !a.Equals(b);
    }
}

internal static class DirectionExtensions
{
    public static Direction Opposite(this Direction direction)
    {
        switch (direction)
        {
            case Direction.Left:
                return Direction.Right;
            case Direction.Right:
                return Direction.Left;
            case Direction.Up:
                return Direction.Down;
            case Direction.Down:
                return Direction.Up;
            default:
                return Direction.None;
        }
    }

    public static Vector2 ToVector(this Direction direction)
    {
        switch (direction)
        {
            case Direction.Left:
                return new Vector2(-1, 0);
            case Direction.Right:
                return new Vector2(1, 0);
            case Direction.Up:
                return new Vector2(0, -1);
            case Direction.Down:
                return new Vector2(0, 1);
            default:
                return Vector2.Zero;
        }
    }
}