using System;

internal class Boss : Component
{
    private MoveToEntity _moveToEntityGoal;

    private int lastGroundedTick;
    public Random rnd = new Random();

    private int health;

    public override void Start()
    {
        if (Parent.RigidBody == null) Parent.AddComponent<RigidBody>();

        if (Parent.BoxCollider == null) Parent.AddComponent<BoxCollider>();

        if (!Parent.HasComponent<MovingEntity>()) Parent.AddComponent<MovingEntity>();

        Parent.GetComponent<MovingEntity>().Sticky = true;

        Parent.RigidBody.Mass = Physics.PlayerMass;
        _moveToEntityGoal = new MoveToEntity(Parent, typeof(Player));
        health = Parent.GetComponent<Damageable>().Health;
    }

    public override void OnCollision(Collision collision)
    {
        if (collision.Other.HasComponent<Player>())
            collision.Other.GetComponent<Damageable>().Damage(50 * (lastGroundedTick / 5), collision.Normal);
    }

    public override void FixedUpdate()
    {
        var rigidBody = Parent.RigidBody;
        var position = rigidBody.Position;
        var movingEntity = Parent.GetComponent<MovingEntity>();
        if (lastGroundedTick < 50 && !rigidBody.Grounded)
        {
            lastGroundedTick++;
        }

        var newHealth = Parent.GetComponent<Damageable>().Health;

        if (newHealth != health)
        {
            SpawnBaby(1);

            health = newHealth;
        }

        //jump
        if (rigidBody.Grounded)
        {
            lastGroundedTick = 0;
            movingEntity.Jump(Physics.PlayerJumpHeight / 2);
        }

        var distanceToTarget = (position - Game.MainPlayer.Transform.Position).Length();
        if (rigidBody.Grounded && distanceToTarget < 96f && rnd.NextDouble() <= 0.25f)
        {
            // slam dunk
            movingEntity.Jump(Physics.PlayerJumpHeight * 2);
            lastGroundedTick = 0;
        }

        // move
        var moves = _moveToEntityGoal.Execute();
        foreach (var move in moves)
            switch (move)
            {
                case MoveDirection.Left:
                    movingEntity.Move(new Vector2(-Physics.PlayerSpeed * Physics.FixedDeltaTime / 2, 0));
                    break;
                case MoveDirection.Right:
                    movingEntity.Move(new Vector2(Physics.PlayerSpeed * Physics.FixedDeltaTime / 2, 0));
                    break;
            }

        health = Parent.GetComponent<Damageable>().Health;
    }

    private void SpawnBaby(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var obj = PrefabManager.Create("BossBaby", Parent.Transform.Position + new Vector2(40, 0));
            GameManager.Load(obj);
        }
    }
}