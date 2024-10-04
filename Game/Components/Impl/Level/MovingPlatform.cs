internal class MovingPlatform : Component
{
    private bool _right;
    public MovingBounds MovingBounds { get; set; } = new MovingBounds { Left = Vector2.Zero, Right = Vector2.Zero };
    public bool Moving { get; set; } = true;

    public override void Start()
    {
        // check for required components
        if (Parent.RigidBody == null) Parent.AddComponent<RigidBody>();

        if (Parent.BoxCollider == null) Parent.AddComponent<BoxCollider>();

        if (!Parent.HasComponent<MovingEntity>()) Parent.AddComponent<MovingEntity>();

        Parent.RigidBody.IsStatic = true;
    }

    public override void FixedUpdate()
    {
        if (!Moving) return;

        var rigidBody = Parent.RigidBody;

        var position = Parent.RigidBody.Position;
        var xPos = position.X;

        var lBound = MovingBounds[0];
        var rBound = MovingBounds[1];

        var movingEntity = Parent.GetComponent<MovingEntity>();

        // if the bounds are the same / too close, don't move
        if ((rBound - lBound).Length() <= 0.1f) return;

        // alternate between moving left & right based on bounds
        switch (_right)
        {
            case true when xPos > rBound.X:
                _right = false;
                break;
            case false when xPos < lBound.X:
                _right = true;
                break;
        }

        // move
        if (_right)
            rigidBody.Position += (new Vector2(Physics.FixedDeltaTime * 100, 0));
        else
            rigidBody.Position += (new Vector2(-Physics.FixedDeltaTime * 100, 0));
    }
}