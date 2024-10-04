internal class Transform : Component
{
    public Vector2 Position { get; set; } // current position

    public Vector2 Size { get; set; } // scale of the entity

    public Bounds2 Bounds => new Bounds2(Position, Size);

    public Vector2 Center
    {
        get => Position + Size / 2; // center of the entity
        set => Position = value - Size / 2;
    }

    public Vector2 Bottom
    {
        get => new Vector2(Position.X, Position.Y + Size.Y);
        set => Position = new Vector2(value.X, value.Y - Size.Y);
    }

    public Vector2 Top
    {
        get => new Vector2(Position.X, Position.Y);
        set => Position = new Vector2(value.X, value.Y);
    }

    public Vector2 Left
    {
        get => new Vector2(Position.X, Position.Y);
        set => Position = new Vector2(value.X, value.Y);
    }

    public Vector2 Right
    {
        get => new Vector2(Position.X + Size.X, Position.Y);
        set => Position = new Vector2(value.X - Size.X, value.Y);
    }
}