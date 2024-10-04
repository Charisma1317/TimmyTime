using System;
using System.Diagnostics;
using System.Linq;

internal class RigidBody : Component
{
    // basic physics stuff
    public Vector2 Position { get; set; } = Vector2.Zero;
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public Vector2 Acceleration { get; set; } = Vector2.Zero;
    public float Mass { get; set; } = 1f;

    public bool IsStatic { get; set; } = false;
    public bool Grounded { get; set; }

    public bool Left { get; set; }

    public override void Start()
    {
        Position = Transform.Position;
    }

    public void ApplyForce(Vector2 force)
    {
        // F = ma, so a = F/m
        var acceleration = force / Mass;
        Acceleration += acceleration;

        if (force.Length() > 0f)
            Parent.SpriteRenderer.Animate = true;
    }

    public void ApplyImpulse(Vector2 impulse)
    {
        // Impulse is an instantaneous change in velocity
        Velocity += impulse / Mass;

        if (impulse.Length() > 0f)
            Parent.SpriteRenderer.Animate = true;
    }

    // Simple gravity implementation
    public void ApplyGravity(float gravityScale)
    {
        // F = mgh
        ApplyForce(new Vector2(0, Mass * Mass * gravityScale));
    }

    private void ApplyFriction()
    {
        var newX = Velocity.X;
        if (Velocity.X > 0f)
            newX = Math.Max(0f, Velocity.X * Physics.FrictionConstant);
        else if (Velocity.X < 0f) newX = Math.Min(0f, Velocity.X * Physics.FrictionConstant);

        Velocity = new Vector2(newX, Velocity.Y);
    }

    public override void Update()
    {
        // update the transform position
        Transform.Position = Position;
        LevelManager.CurrentLevel.UpdateOnGrid(Parent);

        if (IsStatic) return;

        if (Velocity.Y > 0f || Velocity.Y < 0f) Grounded = false;

        // if velocity is too small, just set it to 0
        if (Math.Abs(Velocity.X) < 0.1f) Velocity = new Vector2(0, Velocity.Y);

        if (Math.Abs(Velocity.Y) < 0.1f) Velocity = new Vector2(Velocity.X, 0);
    }

    public void Knockback(Direction direction, float force)
    {
        var knockback = new Vector2(0, 0);
        switch (direction)
        {
            case Direction.Left:
                knockback = new Vector2(-force, -Physics.PlayerJumpHeight * Physics.PlayerMass / 2f);
                break;
            case Direction.Right:
                knockback = new Vector2(force, -Physics.PlayerJumpHeight * Physics.PlayerMass / 2f);
                break;
            case Direction.Up:
                knockback = new Vector2(Physics.PlayerJumpHeight / 4f, -force);
                break;
            case Direction.Down:
                knockback = new Vector2(Physics.PlayerJumpHeight / 4f, force);
                break;
        }

        Debug.WriteLine("knockback: " + knockback);

        Velocity = Vector2.Zero;

        ApplyImpulse(knockback);

        Debug.WriteLine("vel: " + Velocity);
    }

    private void UpdateGrounded()
    {
        if (!Parent.HasComponent<MovingEntity>()) return;

        var collider = Parent.BoxCollider;
        var level = LevelManager.CurrentLevel;
        var spaces = level.GetGridSpaces(Transform.Bounds);
        // get the grid spaces below us
        var dup = spaces.ToList();
        foreach (var s in dup
                     .Where(s => level.UniformGridHeight - 1 != s.Item2))
            spaces.Add((s.Item1, s.Item2 + 1));

        var gridSpaces = level.GetObjectsInSpaces(spaces);
        if (gridSpaces.Count == 0)
        {
            Grounded = false;
            return;
        }

        var update = true;

        foreach (var below in gridSpaces)
        {
            if (below == Parent) continue;
            if (below.HasComponent<RigidBody>() && !below.RigidBody.IsStatic) continue;
            if (below.Label != Label.Other) continue;
            var col = level.CompositeColliders.ContainsKey(below) ? level.CompositeColliders[below] : below.BoxCollider;
            var aabb = col.AABB();

            // how close are we to the ground?
            var distanceY = Math.Abs(below.Transform.Position.Y - Position.Y);
            var leftX = Position.X + collider.Offset.X;
            var rightX = leftX + collider.Size.X;
            var belowLeftX = aabb.Min.X;
            var belowRightX = aabb.Max.X;

            var leftOnPlatform = IsBetween(leftX, belowLeftX, belowRightX);
            var rightOnPlatform = IsBetween(rightX, belowLeftX, belowRightX);

            /*Parent.SpriteRenderer.Draw(() =>
            {
                var b = below;
                var cameraPos = GameManager.MainCamera.Transform.Position;
                var localAABB = new Bounds2(aabb.Position - cameraPos, aabb.Size);
                var transparentColor = Color.White;
                Engine.DrawRectSolid(localAABB, transparentColor);
            });*/

            if ((leftOnPlatform || rightOnPlatform)
                && distanceY <= Parent.Transform.Size.Y)
            {
                update = false;
                break;
            }
        }

        if (update) Grounded = false;
    }

    public override void OnCollision(Collision collision)
    {
        var normal = collision.Normal;
        var dir = GetDirectionFromVector(-normal);

        if (dir == Direction.Down)
            // we hit the ground
            Grounded = true;
    }

    public override void FixedUpdate()
    {
        if (IsStatic) return;

        if (!Grounded)
            ApplyGravity(Physics.GravityConstant);
        else
            UpdateGrounded();

        ApplyFriction();

        if (Velocity.X > 0f)
            Left = false;
        else if (Velocity.X < 0f) Left = true;

        Parent.SpriteRenderer.Animate = false;

        // Integrate the equations of motion
        Position += Velocity * Physics.FixedDeltaTime;

        Velocity += Acceleration * Physics.FixedDeltaTime;

        // Reset acceleration for the next frame
        Acceleration = Vector2.Zero;
    }

    public static Direction GetDirectionFromVector(Vector2 dir)
    {
        if (dir.X < 0f) return Direction.Left;

        if (dir.X > 0f) return Direction.Right;

        if (dir.Y < 0f) return Direction.Up;

        if (dir.Y > 0f) return Direction.Down;

        return Direction.None;
    }

    private bool IsBetween(float value, float min, float max)
    {
        return value >= min && value <= max;
    }
}