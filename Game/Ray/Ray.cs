using System;

internal struct Ray
{
    public Vector2 Origin { get; } // the origin of the ray
    public Vector2 Direction { get; } // the direction of the ray

    public Vector2 InvDirection => new Vector2(
        Direction.X != 0.0f ? 1 / Direction.X : 0,
        Direction.Y != 0.0f ? 1 / Direction.Y : 0);

    public Ray(Vector2 origin, Vector2 direction)
    {
        Origin = origin;
        Direction = direction;
    }

    public (bool, Vector2) Hit(Bounds2 b)
    {
        float tMin = float.NegativeInfinity, tMax = float.PositiveInfinity;

        for (var i = 0; i < 2; i++)
            if (Direction[i] != 0.0)
            {
                var t1 = (b.Min[i] - Origin[i]) * InvDirection[i];
                var t2 = (b.Max[i] - Origin[i]) * InvDirection[i];

                tMin = Math.Max(tMin, Math.Min(t1, t2));
                tMax = Math.Min(tMax, Math.Max(t1, t2));
            }
            else if (Origin[i] < b.Min[i] || Origin[i] > b.Max[i])
            {
                return (false, Vector2.Zero);
            }

        if (tMax >= tMin && tMax >= 0.0) return (true, GetPoint(tMin));

        return (false, Vector2.Zero);
    }

    public Collision Cast(Bounds2 bounds)
    {
        var hit = new Collision();

        var min = bounds.Min;
        var max = bounds.Max;

        var lastEntry = float.NegativeInfinity;
        var firstExit = float.PositiveInfinity;

        for (var i = 0; i < 2; ++i)
            if (Direction[i] != 0)
            {
                var t1 = (min[i] - Origin[i]) / Direction[i];
                var t2 = (max[i] - Origin[i]) / Direction[i];

                lastEntry = Math.Max(lastEntry, Math.Min(t1, t2));
                firstExit = Math.Min(firstExit, Math.Max(t1, t2));
            }
            else if (Origin[i] < min[i] || Origin[i] > max[i])
            {
                return hit;
            }

        if (!(firstExit > lastEntry) || !(firstExit > 0) || !(lastEntry < 1)) return hit;

        hit.Position = Origin + Direction * lastEntry;
        hit.Time = lastEntry;
        hit.IsHit = true;

        var delta = hit.Position - bounds.Center;
        var point = bounds.HalfSize - Vector2.Abs(delta);

        if (point[0] < point[1])
            // (delta[0] > 0) - (delta[0] < 0)
            hit.Normal[0] = Math.Sign(delta[0]);
        else
            hit.Normal[1] = Math.Sign(delta[1]);

        return hit;
    }

    public Vector2 GetPoint(float t)
    {
        return Origin + Direction * t;
    }
}