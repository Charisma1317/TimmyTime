using System;
using System.Diagnostics;


internal enum WeaponType
{
    Bow,
    Melee
}

internal class Player : Component
{
    public WeaponType EquippedWeaponType { get; set; } = WeaponType.Bow;

    public float arrowHeld = 0.1f;
    private bool _fly;

    public void CheckTimer()
    {
        if (Game.LevelTimer >= 180) GameManager.Lose();
    }

    public override void Start()
    {
        // check for required components
        if (Parent.RigidBody == null) Parent.AddComponent<RigidBody>();

        if (Parent.BoxCollider == null) Parent.AddComponent<BoxCollider>();

        if (!Parent.HasComponent<MovingEntity>()) Parent.AddComponent<MovingEntity>();

        if (!Parent.HasComponent<Bow>()) Parent.AddComponent<Bow>();
        if (!Parent.HasComponent<Melee>()) Parent.AddComponent<Melee>();
        if (!Parent.HasComponent<Damageable>()) Parent.AddComponent<Damageable>();

        Parent.GetComponent<RigidBody>().Mass = Physics.PlayerMass;
        Parent.GetComponent<MovingEntity>().Sticky = true;
    }
    
    public override void FixedUpdate()
    {
        var movingEntity = Parent.GetComponent<MovingEntity>();
        var move = Vector2.Zero;

        if (_fly)
        {
            HandleFlyControls();
            return;
        }

        // arbitrary movement speed
        if (Engine.GetKeyHeld(Key.D) && !movingEntity.WallStick.Item3.Equals(Direction.Right))
            move = new Vector2(Physics.PlayerSpeed * Physics.FixedDeltaTime, 0);

        if (Engine.GetKeyHeld(Key.A) && !movingEntity.WallStick.Item3.Equals(Direction.Left))
            move = new Vector2(-Physics.PlayerSpeed * Physics.FixedDeltaTime, 0);

        if (Engine.GetKeyHeld(Key.S) && movingEntity.WallStick.Item1)
        {
            movingEntity.Drop();
        }

        // if the player is wall sticking, reduce their y velocity to Physics.PlayerWallFallOffRate
        var rigidBody = Parent.RigidBody;

        var wallSticking = movingEntity.WallStick.Item1;
        if (!wallSticking)
        {
            if (rigidBody.Grounded)
                movingEntity.Move(move);
            else
                rigidBody.ApplyImpulse(Physics.FixedDeltaTime * move * Physics.PlayerSpeed);

            return;
        }

        HandleWallStick(move);
    }

    public override void Update()
    {
        var movingEntity = Parent.GetComponent<MovingEntity>();
        var rigidBody = Parent.RigidBody;

        //New stuff
        if (Engine.GetKeyUp(Key.E))
        {
            HandleMelee();
        }

        CheckTimer();

        if (Engine.GetKeyDown(Key.F))
        {
            _fly = !_fly;
            rigidBody.IsStatic = _fly;
            return;
        }

        //If you fall off the face of the planet you lose
        if (rigidBody.Position.Y > 600) GameManager.Lose();

        if (Engine.GetKeyDown(Key.W)) Debug.WriteLine($"G: {rigidBody.Grounded}, vel: {rigidBody.Velocity}");

        if (Engine.GetKeyDown(Key.W) && rigidBody.Grounded)
            movingEntity.Jump();

        var wallSticking = movingEntity.WallStick.Item1;
        if (Engine.GetKeyDown(Key.W) && wallSticking) movingEntity.ClimbWall();

        // if still wall sticking, then wall jump in the opposite direction
        if (Engine.GetKeyDown(Key.W) && wallSticking)
        {
            var direction = movingEntity.WallStick.Item3;
            var knockback = new Vector2(0, 0);
            switch (direction)
            {
                case Direction.Left:
                    knockback = new Vector2(10, -Physics.PlayerJumpHeight * Physics.PlayerMass);
                    break;
                case Direction.Right:
                    knockback = new Vector2(-10, -Physics.PlayerJumpHeight * Physics.PlayerMass);
                    break;
            }

            rigidBody.Velocity = Vector2.Zero;
            rigidBody.ApplyImpulse(knockback);
        }
    }

    public override void OnCollision(Collision collision)
    {
        var damagable = Parent.GetComponent<Damageable>();
        if (collision.Other.Label != Label.Enemy) return;

        damagable.Damage(35, collision.Normal);
    }

    private void HandleFlyControls()
    {
        var rigidBody = Parent.RigidBody;
        var move = Vector2.Zero;
        if (Engine.GetKeyHeld(Key.W)) move += new Vector2(0, -1);

        if (Engine.GetKeyHeld(Key.S)) move += new Vector2(0, 1);

        if (Engine.GetKeyHeld(Key.A)) move += new Vector2(-1, 0);

        if (Engine.GetKeyHeld(Key.D)) move += new Vector2(1, 0);

        rigidBody.Position += move * 4f;
    }

    //god forbid make this not ugly
    private void HandleWallStick(Vector2 move)
    {
        var rigidBody = Parent.RigidBody;
        var movingEntity = Parent.GetComponent<MovingEntity>();

        var velocity = rigidBody.Velocity;

        rigidBody.Velocity = new Vector2(velocity.X, Physics.PlayerWallFallOffRate);

        // if the player is moving the opposite direction of the wall, unstick them
        var direction = movingEntity.WallStick.Item3;
        var oppositeDir = direction.Opposite();
        var playerDirVector = rigidBody.Velocity.Normalized();
        var playerDirX = playerDirVector.X;


        if ((playerDirX < 0f && oppositeDir == Direction.Left) ||
            (playerDirX > 0f && oppositeDir == Direction.Right))
        {
            movingEntity.WallStick = (false, null, Direction.None);
            movingEntity.Move(move);
            return;
        }

        if ((playerDirX < 0f && direction == Direction.Left) ||
            (playerDirX > 0f && direction == Direction.Right))
        {
            // check if the player is moving into the wall
            var wall = movingEntity.WallStick.Item2;
            var wallPos = wall.bounds.Position;
            var block = wall.GetClosestObjectFromPoint(wallPos);
            var blockPos = block.Transform.Position;
            var blockSize = block.Transform.Size;

            if ((direction != Direction.Right || !(blockPos.X < Parent.Transform.Position.X)) &&
                (direction != Direction.Left || !(blockPos.X + blockSize.X > Parent.Transform.Position.X))) return;
            movingEntity.WallStick = (false, null, Direction.None);
            movingEntity.Move(move);
            return;
        }

        movingEntity.Move(move);
    }


    //ehh not that holy but not entirely unholy
    //todo: move to the melee component
    private void HandleMelee()
    {
        var rigidBody = Parent.RigidBody;
        var enemyResult = Parent.FindNearestObject(2, Label.Enemy);

        if (!enemyResult.Found) return;
        var dist = enemyResult.Distance;
        var dirToKnockback = new Vector2(rigidBody.Left ? -1 : 1, 0);
        var enemy = enemyResult.Result;

        if (!enemy.HasComponent<Damageable>()) return;
        var damageable = enemy.GetComponent<Damageable>();

        if (dist < 50f)
        {
            damageable.Damage(75, dirToKnockback);
        }
    }
}