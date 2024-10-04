internal class WinTrigger : Component
{
    public static readonly float TimeInLevel = 180f;

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
        Physics.setJump();
        Physics.setSpeed();
        var e = collision.Other;
        if (!e.HasComponent<Player>()) return;

        if (LevelManager.HasNextLevel())
            LevelManager.LoadNextLevel();
        else
            GameManager.Win();

        Parent.DestroyInternal();
    }
}