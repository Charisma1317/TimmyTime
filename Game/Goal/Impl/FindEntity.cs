using System;
using System.Collections.Generic;
using System.Linq;

internal class FindEntity : Goal<bool>
{
    private readonly Type _toFindType;
    private GameObject _toFind;

    public FindEntity(GameObject parent, Type toFindType) : base(parent)
    {
        _toFindType = toFindType;
    }

    public override bool Execute()
    {
        _toFind = GameManager.LoadedObjectsByType[_toFindType].FirstOrDefault();

        if (_toFind == null) return false;

        var aiEntity = _parent.GetComponent<AIEntity>();
        var range = aiEntity.Range;

        var rigidBody = _parent.GetComponent<RigidBody>();
        var moveToBody = _toFind.GetComponent<RigidBody>();

        var path = moveToBody.Position - rigidBody.Position;
        
        return path.Length() <= range; // very simple check, just check if the player is within range, good enough for now

        // COMPLEX CHECK TO SEE IF THERE IS A BLOCK IN THE WAY
        /*var dir = path.Normalized();

        var level = LevelManager.CurrentLevel;

        var cellsToCheck = level.GetGridSpaces(_parent.Transform.Bounds);

        cellsToCheck.UnionWith(FindGridCells(
            (int)(rigidBody.Position.X / 64),
            (int)(rigidBody.Position.Y / 64),
            (int)(moveToBody.Position.X / 64),
            (int)(moveToBody.Position.Y / 64)
        ));

        var objects = LevelManager.CurrentLevel.GetObjectsInSpaces(cellsToCheck).ToHashSet();

        var origin = new Vector2(_parent.Transform.Center.X, _parent.Transform.Center.Y);

        var ray = new Ray(origin, dir);

        var distMap = new Dictionary<GameObject, float>();
        var aabb = _parent.Transform.Bounds;

        foreach (var gameObj in objects)
        {
            if (gameObj == _parent) continue;
            var otherAABB = gameObj.Transform.Bounds;
            var sumAABB = new Bounds2
            {
                HalfSize = otherAABB.HalfSize + aabb.HalfSize,
                Center = otherAABB.Center
            };

            var c = ray.Cast(sumAABB);

            if (!c.IsHit) continue;

            var dist = (origin - c.Position).Length();
            distMap.Add(gameObj, dist);
        }

        var orderedGameObjs = distMap.OrderBy(k => k.Value).ToList();
        foreach (var (gameObj, dist) in orderedGameObjs)
            if (gameObj.HasComponent<Block>())
                return false;

        return true;*/
    }

    private HashSet<(int, int)> FindGridCells(int x, int y, int x2, int y2)
    {
        var gridCells = new HashSet<(int, int)>();

        var w = x2 - x;
        var h = y2 - y;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        if (w < 0) dx1 = -1;
        else if (w > 0) dx1 = 1;
        if (h < 0) dy1 = -1;
        else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1;
        else if (w > 0) dx2 = 1;
        var longest = Math.Abs(w);
        var shortest = Math.Abs(h);
        if (!(longest > shortest))
        {
            longest = Math.Abs(h);
            shortest = Math.Abs(w);
            if (h < 0) dy2 = -1;
            else if (h > 0) dy2 = 1;
            dx2 = 0;
        }

        var numerator = longest >> 1;
        for (var i = 0; i <= longest; i++)
        {
            gridCells.Add((x, y));
            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }

        return gridCells;
    }
}