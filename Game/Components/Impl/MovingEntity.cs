using System;

internal class MovingEntity : Component
{
    public (bool, CompositeCollider, Direction) WallStick { get; set; } =
        (false, null, Direction.None); // is this entity stuck to walls rn?

    public bool Sticky { get; set; } = false; // can this entity stick to walls?

    public bool _fall;

    public void Jump(float height = 275f)
    {
        Parent.RigidBody.ApplyImpulse(new Vector2(0, -height * Physics.PlayerMass));
    }

    public void Move(Vector2 direction)
    {
        var rigidBody = Parent.RigidBody;
        var mass = rigidBody.Mass;
        Parent.RigidBody.ApplyForce(Physics.FixedDeltaTime * direction * Physics.PlayerSpeed * mass);
    }

    public void Drop()
    {
        if (_fall && !WallStick.Item1) return;

        WallStick = (false, null, Direction.None);
        Sticky = false;
    }

    public void ClimbWall()
    {
        var rigidBody = Parent.RigidBody;
        var collider = Parent.GetComponent<BoxCollider>();
        var size = collider.Size;

        var wall = WallStick.Item2;
        var direction = WallStick.Item3;

        // wall is a composite collider
        // get top of the wall
        var wallPos = wall.bounds.Position;
        var wallSize = wall.bounds.Size;
        var topBlock = wall.GetClosestObjectFromPoint(wallPos);

        if (direction == Direction.Left)
            topBlock = wall.GetClosestObjectFromPoint(wallPos + new Vector2(wallSize.X, 0));

        if (topBlock == null) return;

        // if top block is obstructed, cancel
        var topBlockPos = topBlock.Transform.Position;
        var topBlockSize = topBlock.Transform.Size;

        var colliderPosX = Transform.Position.X + collider.Offset.X;
        var colliderSizeX = size.X;

        // check if the top block is on the same x axis as the player
        if (Math.Abs(topBlockPos.X - colliderPosX) < colliderSizeX) return;

        // if the top of the wall is close enough to the player, just teleport them to the top
        if (Math.Abs(rigidBody.Position.Y - topBlockPos.Y) >= 20) return;
        var newX = topBlockPos.X;

        rigidBody.Position = new Vector2(newX, topBlockPos.Y - size.Y - 5f);
        WallStick = (false, null, Direction.None);
    }

    public void UpdateWallStick(RigidBody body)
    {
        if (body.Grounded)
        {
            WallStick = (false, null, Direction.None);
            return;
        }

        // if velocity is opposite of wall direction, unstick
        var direction = WallStick.Item3;
        var oppositeDir = direction.Opposite();
        var playerDirVector = body.Velocity.Normalized();
        var playerDirX = playerDirVector.X;

        if ((playerDirX < 0f && direction == Direction.Left) ||
            (playerDirX > 0f && direction == Direction.Right))
            return;

        if ((playerDirX < 0f && oppositeDir == Direction.Left) ||
            (playerDirX > 0f && oppositeDir == Direction.Right))
            WallStick = (false, null, Direction.None);

        if (!WallStick.Item1) return;

        if (!WallStick.Item2.AABB().Overlaps(Parent.GetComponent<BoxCollider>().AABB()))
            WallStick = (false, null, Direction.None);
    }

    public override void OnCollision(Collision collision)
    {
        var other = collision.Other;
        if (!other.HasComponent<Block>()) return;

        var body = Parent.RigidBody;

        var wallSticking = WallStick.Item1;
        var dir = RigidBody.GetDirectionFromVector(-collision.Normal);
        if (body.Grounded || _fall || !(dir == Direction.Left || dir == Direction.Right)) return;

        if (wallSticking) return;

        WallStick = (true, collision.OtherCollider as CompositeCollider, dir);

        UpdateWallStick(body);
    }

    public override void FixedUpdate()
    {
        if (!Sticky) return;

        UpdateWallStick(Parent.RigidBody);
    }

    private int f = 0;

    public override void Update()
    {
        if (Parent.HasComponent<Player>())
        {
            f++;
            // log our y axis into a file
            var pos = Parent.Transform.Position;
            var y = pos.Y;
        
            var s = $"{f},{y}\n";
            System.IO.File.AppendAllText("y.txt", s);
        }

        var rigidBody = Parent.RigidBody;

        if (rigidBody.Grounded && _fall)
        {
            _fall = false;
            Sticky = true;
        }
    }

    public override void Start()
    {
        // ensure this gameobject has the required components
        if (Parent.RigidBody == null) Parent.AddComponent<RigidBody>();

        if (Parent.BoxCollider == null) Parent.AddComponent<BoxCollider>();
    }
}