using System;
using System.Diagnostics;

internal class Arrow : Component
{
    public float Power { get; set; } = 0f;
    public Vector2 EndPosition { get; set; }

    private float _power;

    private bool _poison = false;

    public override void Start()
    {
        if (Parent.RigidBody == null) Parent.AddComponent<RigidBody>();

        if (Parent.BoxCollider == null) Parent.AddComponent<BoxCollider>();

        if (!Parent.HasComponent<MovingEntity>()) Parent.AddComponent<MovingEntity>();

        Parent.GetComponent<RigidBody>().Mass = 16f;
        Parent.RigidBody.IsStatic = true; // until shot
        Parent.BoxCollider.IsTrigger = true; // until shot
    }

    public override void OnCollision(Collision collision)
    {
        var rigidBody = Parent.RigidBody;
        var other = collision.Other;

        var dirToKnockback = new Vector2(rigidBody.Left ? 1 : -1, 0);

        Parent.Destroy();

        if (!other.HasComponent<Damageable>()) return;
        var damageable = other.GetComponent<Damageable>();
        damageable.Damage((int)(10 * _power), dirToKnockback);
        Debug.WriteLine(String.Format("{0} damage done to Obj(label={1}, prefab={2})", (int)(10 * _power), Enum.GetName(typeof(Label), other.Label), other.OriginalPrefab));
        if (_poison)
        {
            damageable.DoT.Set(10, 3);
        }
    }

    public override void FixedUpdate()
    {
        // move towards end position
        if (Power > 0f)
        {
            _power = Power;
            var rigidBody = Parent.RigidBody;
            var transform = Parent.Transform;

            var dir = (EndPosition - transform.Position).Normalized();
            var vel = dir * 1000f * Physics.FixedDeltaTime;

            rigidBody.Position += vel;

            rigidBody.Acceleration = Vector2.Zero;

            if (Vector2.Distance(rigidBody.Position, EndPosition) < 16f)
            {
                rigidBody.ApplyImpulse(new Vector2(vel.X, vel.Y < 0 ? -vel.Y : vel.Y) * 1000f);
                Power = 0f;
            }
        }

        if (Transform.Position.Y > 600f)
        {
            Parent.Destroy();
        }
    }
    public void SetPoisonous(bool toggle = true)
    {
        _poison = toggle;
    }
}