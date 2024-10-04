internal class SimpleMovingEnemy : Component
{
    private bool _goingRight = true;

    private static Vector2 MovingVelocity { get; } = new Vector2(Physics.FixedDeltaTime * Physics.PlayerSpeed, 0);

    public MovingBounds MovingBounds { get; set; } = new MovingBounds { Left = Vector2.Zero, Right = Vector2.Zero };

    public override void Start()
    {
        if (Parent.RigidBody == null) Parent.AddComponent<RigidBody>();

        if (Parent.BoxCollider == null) Parent.AddComponent<BoxCollider>();

        if (!Parent.HasComponent<MovingEntity>()) Parent.AddComponent<MovingEntity>();

        Parent.GetComponent<RigidBody>().Mass = Physics.PlayerMass;

        if (MovingBounds == null) MovingBounds = MovingBounds.Default;
    }

    public override void OnCollision(Collision collision)
    {
        if (collision.Other.HasComponent<Damageable>()) collision.Other.GetComponent<Damageable>().Damage(35, collision.Normal);
    }

    public override void FixedUpdate()
    {
        var rigidBody = Parent.RigidBody;
        var movingEntity = Parent.GetComponent<MovingEntity>();
        var position = rigidBody.Position;

        switch (_goingRight)
        {
            case false:
                movingEntity.Move(-MovingVelocity);
                break;
            case true:
                movingEntity.Move(MovingVelocity);
                break;
        }

        if (position.X >= MovingBounds[1].X && _goingRight)
            _goingRight = false;
        else if (position.X <= MovingBounds[0].X && !_goingRight) _goingRight = true;


        if (MovingBounds[0].X <= Parent.RigidBody.Position.X && MovingBounds[1].X >= Parent.RigidBody.Position.X &&
            Parent.RigidBody.Position.Y == MovingBounds[1].Y)
        {
            if (rigidBody.Position.X < Parent.RigidBody.Position.X && _goingRight)
                rigidBody.ApplyForce(new Vector2(5, 0));
            else if (rigidBody.Position.X > Parent.RigidBody.Position.X && !_goingRight)
                rigidBody.ApplyForce(new Vector2(-5, 0));
        }
        else
        {
            rigidBody.ApplyForce(new Vector2(0, 0));
        }
    }
}