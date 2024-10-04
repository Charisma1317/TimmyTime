internal class Portal : Component
{
    public GameObject PartnerPortal;

    public override void Start()
    {
        // check for required components
        if (Parent.RigidBody == null) Parent.AddComponent<RigidBody>();

        if (Parent.BoxCollider == null) Parent.AddComponent<BoxCollider>();

        var col = Parent.GetComponent<BoxCollider>();
        col.IsTrigger = true;
    }

    public Vector2 ExitPosition(RigidBody body)
    {
        var partner = PartnerPortal.GetComponent<Portal>();
        // left of us or right of us depending on the entry position
        return partner.Parent.RigidBody.Position;
    }

    public override void OnTrigger(Collision collision)
    {
        var rigidBody = collision.Other.RigidBody;
        if (rigidBody == null) return;
        // rigidBody.Position = _partner.ExitPosition(rigidBody);
    }
}