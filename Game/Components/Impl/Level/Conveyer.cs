using System;
using System.Diagnostics;

internal class Conveyer : Component
{
    public bool Right { get; set; } = false;

    public BoxCollider Trigger { get; set; }

    public override void Start()
    {
        // check for required components
        if (Parent.RigidBody == null) Parent.AddComponent<RigidBody>();

        Debug.Assert(Parent.RigidBody != null, "Parent.RigidBody != null");

        Parent.RigidBody.IsStatic = true;

        if (Trigger != null) return;
        
        Trigger = Parent.AddComponent<BoxCollider>();
        Trigger.Offset = new Vector2(0, -Parent.Transform.Size.Y);
        Trigger.IsTrigger = true;
    }


    public override void OnTrigger(Collision collision)
    {
        var rigidBody = collision.Other.RigidBody;
        if (rigidBody == null) return;

        if (!rigidBody.Grounded) return;

        var direction = Right ? 1 : -1;

        rigidBody.ApplyForce(new Vector2(Physics.PlayerSpeed * Math.Sign(direction) * 4, 0));
    }

    //todo: add ontriggerexit
}