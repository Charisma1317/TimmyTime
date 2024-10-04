using System;
using System.Collections.Generic;
using System.Text;

class Powerup : Component
{
    public override void Start()
    {
        // check for required components
        if (Parent.RigidBody == null) Parent.AddComponent<RigidBody>();

        if (Parent.BoxCollider == null) Parent.AddComponent<BoxCollider>();

        var col = Parent.GetComponent<BoxCollider>();
        col.IsTrigger = true;
    }
    public override void OnTrigger(Collision collision)
    {
        var rigidBody = collision.Other.RigidBody;
        var e = collision.Other;
        if (!e.HasComponent<Player>()) return;

        Physics.changeSpeed(1.1f);
        Physics.changeJump(1.05f);

        Parent.DestroyInternal();
    }
}
