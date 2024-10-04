using System.Collections.Generic;

internal abstract class Collider : Component
{
    public Vector2 Offset { get; set; } = Vector2.Zero;
    public Vector2 Size { get; set; } = Vector2.Zero;
    public bool IsTrigger { get; set; } = false;

    public override void Start()
    {
        if (Size.Equals(Vector2.Zero)) Size = Parent.Transform.Size;
    }

    public abstract List<Vector2> GetContacts(Bounds2 other);

    public abstract List<Vector2> Vertices();

    public abstract Bounds2 AABB();
}