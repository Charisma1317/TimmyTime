internal class Ladder : Component
{
    public BoxCollider Trigger { get; set; }

    public override void Start()
    {
        // check for required components
        if (Parent.RigidBody == null) Parent.AddComponent<RigidBody>();

        Parent.RigidBody.IsStatic = true;

        if (Trigger != null) return;

        Trigger = Parent.AddComponent<BoxCollider>();
        Trigger.Size = Parent.Transform.Size + new Vector2(4, 0);
        Trigger.Offset = new Vector2(-2, 0);
        Trigger.IsTrigger = true;
    }

    public override void OnTrigger(Collision collision)
    {
        var rigidBody = collision.Other.RigidBody;
        var movingEntity = collision.Other.GetComponent<MovingEntity>();

        if (rigidBody == null) return;

        /*if (!rigidBody.Grounded)
        {
            return;
        }*/


        if (Engine.GetKeyHeld(Key.W))
            rigidBody.Velocity = new Vector2(0, -Physics.LadderSpeed);
        else if (Engine.GetKeyHeld(Key.S))
            rigidBody.Velocity = new Vector2(0, Physics.LadderSpeed);
        else if (Engine.GetKeyHeld(Key.D))
            rigidBody.Velocity = new Vector2(Physics.LadderSpeed, 0);
        else if (Engine.GetKeyHeld(Key.A))
            rigidBody.Velocity = new Vector2(-Physics.LadderSpeed, 0);
        else
            rigidBody.Velocity = Vector2.Zero;
        //rigidBody.ApplyForce(new Vector2(0, Physics.PlayerSpeed * Math.Sign(-1) * 4));
    }
}