using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Single;

internal class BoxCollider : Collider
{
    public override void Update()
    {
        if (Parent.RigidBody == null) return;
        if (Parent.RigidBody.IsStatic) return;

        var newVel = Parent.RigidBody.Velocity + Parent.RigidBody.Acceleration * Physics.FixedDeltaTime;
        if (newVel.Length() == 0.0f) return;

        var rigidBody = Parent.RigidBody;

        var level = LevelManager.CurrentLevel;

        var objects = level.GetObjectsAroundObject(Parent);
        SweepCheck(objects, rigidBody, newVel);
        AABBCheck(objects, rigidBody);
    }

    private void SweepCheck(List<GameObject> objects, RigidBody body, Vector2 velocity)
    {
        var collision = Sweep(objects, body, velocity);

        if (!collision.IsHit) return;

        if (collision.Time < 0 || collision.Time > 1) return;

        // move out of center
        collision.Position -= Parent.Transform.Size / 2f;

        if (collision.OtherCollider.IsTrigger)
        {
            Parent.OnTrigger(collision);

            // other trigger
            collision.Other.OnTrigger(new Collision
            {
                Other = Parent,
                OtherCollider = this,
                Position = collision.Position,
                Normal = collision.Normal,
                Time = collision.Time,
                IsHit = true
            });
        }
        else
        {
            Parent.OnCollision(collision);
        }
    }

    private void AABBCheck(List<GameObject> objects, RigidBody body)
    {
        var level = LevelManager.CurrentLevel;
        foreach (var gameObj in objects)
        {
            if (gameObj == Parent) continue;
            if (gameObj == null) continue;
            if (!level.CompositeColliders.ContainsKey(gameObj) && !gameObj.HasComponent<BoxCollider>()) continue;
            if (MovingPlatformHack(gameObj, body)) continue;

            foreach (var collider in GetColliders(level, gameObj))
            {
                var aabb = AABB();
                var otherAABB = collider.AABB();
                var diff = otherAABB.MinkowskiDifference(aabb);
                var min = diff.Min;
                var max = diff.Max;

                if (!(min[0] <= 0) || !(max[0] >= 0) || !(min[1] <= 0) || !(max[1] >= 0)) continue;

                var penetration = diff.PenetrationVector();
                if (penetration.Length() == 0.0f) continue;

                var collision = new Collision
                {
                    IsHit = true,
                    Other = gameObj,
                    OtherCollider = collider,
                    Position = body.Position + penetration,
                    Normal = penetration.Normalized(),
                    Time = 0f
                };

                if (!collider.IsTrigger)
                {
                    Parent.OnCollision(collision);
                }
                else
                {
                    Parent.OnTrigger(collision);

                    gameObj.OnTrigger(new Collision
                    {
                        Other = Parent,
                        OtherCollider = this,
                        Position = body.Position + penetration,
                        Normal = penetration.Normalized(),
                        Time = 0f
                    });
                }
            }
        }
    }

    private Collision Sweep(List<GameObject> objectsToSweep, RigidBody body, Vector2 velocity)
    {
        var level = LevelManager.CurrentLevel;
        var col = new Collision
        {
            Time = PositiveInfinity
        };

        var aabb = AABB();

        foreach (var gameObj in objectsToSweep)
        {
            if (gameObj == Parent) continue;
            if (gameObj == null) continue;
            if (!level.CompositeColliders.ContainsKey(gameObj ?? null) && !gameObj.HasComponent<BoxCollider>()) continue;

            if (MovingPlatformHack(gameObj, body)) continue;

            foreach (var collider in GetColliders(level, gameObj))
            {
                var otherAABB = collider.AABB();
                var sumAABB = new Bounds2
                {
                    HalfSize = otherAABB.HalfSize + aabb.HalfSize,
                    Center = otherAABB.Center
                };

                var ray = new Ray(aabb.Center, velocity.Normalized());
                var hit = ray.Cast(sumAABB);
                if (!hit.IsHit) continue;

                if (hit.Time < col.Time)
                {
                    col = hit;
                }
                else if (Math.Abs(hit.Time - col.Time) < 0.0001f)
                {
                    if (Math.Abs(velocity[0]) > Math.Abs(velocity[1]) && hit.Normal[0] != 0f)
                        col = hit;
                    else if (Math.Abs(velocity[1]) > Math.Abs(velocity[0]) && hit.Normal[1] != 0f) col = hit;
                }

                col.Other = gameObj;
                col.OtherCollider = collider;
            }
        }

        return col;
    }

    // todo: make less expensive (if possible?)
    public List<BoxCollider> GetColliders(Level level, GameObject obj)
    {
        var colliders = new List<BoxCollider>();
        if (level.CompositeColliders.TryGetValue(obj, out var collider)) colliders.Add(collider);

        colliders.AddRange(obj.GetComponents<BoxCollider>());
        return colliders;
    }

    private bool MovingPlatformHack(GameObject gameObj, RigidBody body)
    {
        if (!gameObj.HasComponent<MovingPlatform>() || !Parent.HasComponent<Player>()) return false;

        var playerBottom = body.Position[1] + Size[1];
        var platformTop = gameObj.Transform.Position[1] + 5f;

        return playerBottom > platformTop;
    }

    // get the AABB of this collider
    public override Bounds2 AABB()
    {
        return new Bounds2(Parent.RigidBody.Position + Offset, Size);
    }

    public override void OnCollision(Collision collision)
    {
        // get original & collided entities
        var original = Parent;

        var rigidBody = original.RigidBody;

        var vel = rigidBody.Velocity;

        // move to the point of collision
        rigidBody.Position = collision.Position;

        if (collision.Normal[0] != 0)
            rigidBody.Velocity = new Vector2(0, rigidBody.Velocity[1]);
        else if (collision.Normal[1] != 0) rigidBody.Velocity = new Vector2(rigidBody.Velocity[0], 0);
    }

    public override List<Vector2> GetContacts(Bounds2 col)
    {
        //todo: implement this
        return new List<Vector2>();
    }

    // method to get all the vertices of the collider
    public override List<Vector2> Vertices()
    {
        var bounds = AABB();

        var x1 = bounds.Min.X;
        var x2 = bounds.Max.X;
        var y1 = bounds.Min.Y;
        var y2 = bounds.Max.Y;

        var vertices = new List<Vector2>
        {
            new Vector2(x1, y1),
            new Vector2(x2, y1),
            new Vector2(x2, y2),
            new Vector2(x1, y2)
        };

        return vertices;
    }
}