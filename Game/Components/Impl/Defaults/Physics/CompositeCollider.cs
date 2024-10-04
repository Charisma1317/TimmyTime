using System.Collections.Generic;
using static Grid;
using System.Linq;

internal class CompositeCollider : BoxCollider
{
    // list of colliders that make up this composite collider
    public Bounds2 bounds = Bounds2.Zero;

    public List<GameObject> Objects = new List<GameObject>();

    public override void Update()
    {
    }

    public override void FixedUpdate()
    {
    }

    public GameObject GetClosestObjectFromPoint(Vector2 pos)
    {
        var closest = Objects[0];
        var closestDist = (pos - closest.Transform.Position).Length();

        foreach (var obj in Objects)
        {
            var dist = (pos - obj.Transform.Position).Length();
            if (!(dist < closestDist)) continue;
            closest = obj;
            closestDist = dist;
        }

        return closest;
    }


    public static void CreateCompositeCollider(Level level, Dictionary<GameObject, CompositeCollider> compositeColliders, List<GridCell> contiguousCells)
    {
        // print contiguous cells list, using their x, y coordinates
        var b = CalculateBounds(contiguousCells);

        contiguousCells = contiguousCells.FindAll(cell => !compositeColliders.ContainsKey(cell.GameObject)).ToList();

        var compositeCollider = new CompositeCollider
        {
            bounds = b,
            Objects = contiguousCells.Select(cell => cell.GameObject).ToList()
        };

        foreach (var cell in contiguousCells) compositeColliders.Add(cell.GameObject, compositeCollider);

        level.AddComponent(compositeCollider);
    }

    public override Bounds2 AABB()
    {
        var a = new Bounds2(bounds.Position + Transform.Position, bounds.Size);
        return a;
    }

    private static Bounds2 CalculateBounds(List<GridCell> contiguousCells)
    {
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);

        foreach (var bounds in contiguousCells
                     .Select(cell => cell.GameObject.Transform.Bounds))
        {
            min = Vector2.Min(min, bounds.Min);
            max = Vector2.Max(max, bounds.Max);
        }

        return new Bounds2(new Vector2(min.X, min.Y), new Vector2(max.X - min.X, max.Y - min.Y));
    }
}