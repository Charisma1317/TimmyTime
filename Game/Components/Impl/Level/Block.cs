internal class Block : Component
{
    public override void Start()
    {
        // check for required components
        if (Parent.RigidBody == null) Parent.AddComponent<RigidBody>();
    }
}