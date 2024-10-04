using System.Collections.Generic;
using System.Linq;

internal struct FiniteRay
{
    public Vector2 Origin { get; } // the origin of the ray
    public Vector2 Direction { get; } // the direction of the ray
    public float Length { get; } // the length of the ray

    public Vector2 InvDirection => new Vector2(
        Direction.X != 0.0f ? 1 / Direction.X : float.PositiveInfinity,
        Direction.Y != 0.0f ? 1 / Direction.Y : float.PositiveInfinity);

    public FiniteRay(Vector2 origin, Vector2 direction, float length)
    {
        Origin = origin;
        Direction = direction;
        Length = length;
    }

    private Bounds2 CreateRayBounds()
    {
        var bounds = new Bounds2(Origin - new Vector2(0, 2), new Vector2(Length, 4));
        if (Direction.X < 0)
        {
            // left
            // transform bounds origin to match direction
            bounds.Position = bounds.Position - new Vector2(Length, 0);
            return bounds;
        }

        if (Direction.X > 0)
        {
            return bounds;
        }

        bounds = new Bounds2(Origin - new Vector2(2, 0), new Vector2(4, Length));
        if (Direction.Y > 0) bounds.Position = bounds.Position - new Vector2(0, Length);

        return bounds;
    }

    public List<(GameObject, float)> Hit()
    {
        var hitList = new List<(GameObject, float)>();

        var bounds = CreateRayBounds();
        // get all entities
        var gameObjects = GameManager.GetObjectsWithinBounds(bounds);

        var ray = new Ray(Origin, Direction);

        foreach (var gameObject in gameObjects)
        {
            if (gameObject.HasComponent<Camera>() || gameObject.Equals(LevelManager.CurrentLevel)) continue;
            // if we're not colliding with the entity, skip it
            var (hit, point) = ray.Hit(gameObject.Transform.Bounds);
            if (!hit) continue;

            var dist = (point - Origin).Length();
            if (dist > Length) continue;

            // we hit the entity
            hitList.Add((gameObject, dist));
        }

        hitList = hitList.OrderBy(o => o.Item2).ToList();

        return hitList;
    }

    public Dictionary<GameObject, Vector2> HitPos()
    {
        var hitList = new Dictionary<GameObject, Vector2>();
        // get all entities
        var gameObjects = GameManager.LoadedObjects.Values;

        var ray = new Ray(Origin, Direction);

        foreach (var gameObject in gameObjects)
        {
            if (gameObject.HasComponent<Camera>() || gameObject.Equals(LevelManager.CurrentLevel)) continue;
            // if we're not colliding with the entity, skip it
            var (hit, point) = ray.Hit(gameObject.Transform.Bounds);
            if (!hit) continue;

            var dist = (point - Origin).Length();
            if (dist > Length) continue;

            // we hit the entity
            hitList.Add(gameObject, point);
        }

        return hitList;
    }
}