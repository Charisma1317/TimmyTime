internal class AIEntity : Component
{
    private FindEntity _findEntity;
    private MoveToEntity _moveToEntity;

    public float Range { get; set; } = 320; // 10 tiles * 10 tiles all directions (tile = 32px x 32px)

    private bool toggle = false;

    public override void Start()
    {
        _findEntity = new FindEntity(Parent, typeof(Player));
        _moveToEntity = new MoveToEntity(Parent, typeof(Player));

        if (Parent.RigidBody == null) Parent.AddComponent<RigidBody>();
        if (Parent.BoxCollider == null) Parent.AddComponent<BoxCollider>();

        if (!Parent.HasComponent<MovingEntity>()) Parent.AddComponent<MovingEntity>();

        Parent.GetComponent<RigidBody>().Mass = Physics.PlayerMass;
        Parent.GetComponent<MovingEntity>().Sticky = true;
    }

    public override void OnCollision(Collision collision)
    {
        if (collision.Other.HasComponent<Damageable>() && collision.Other.Label != Label.Enemy)
            collision.Other.GetComponent<Damageable>().Damage(35, -collision.Normal);
    }

    public override void Update()
    {
        if (Engine.GetKeyDown(Key.H))
            toggle = !toggle;
    }

    public override void FixedUpdate()
    {
        if (!_findEntity.Execute()) return;
        var toMove = _moveToEntity.Execute();

        var movingEntity = Parent.GetComponent<MovingEntity>();
        var rigidBody = Parent.RigidBody;

        if (toggle)
            return;

        foreach (var curMoveDir in toMove)
            switch (curMoveDir)
            {
                case MoveDirection.Jump:
                {
                    if (rigidBody.Grounded)
                        movingEntity.Jump();
                    else if (movingEntity.WallStick.Item1)
                        movingEntity.ClimbWall();

                    break;
                }
                case MoveDirection.Right:
                {
                    movingEntity.Move(new Vector2(Physics.PlayerSpeed * Physics.FixedDeltaTime * 0.625f, 0));
                    break;
                }
                case MoveDirection.Left:
                {
                    movingEntity.Move(new Vector2(-Physics.PlayerSpeed * Physics.FixedDeltaTime * 0.625f, 0));
                    break;
                }
            }
    }
}