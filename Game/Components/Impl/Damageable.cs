using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

public class DoTInfo
{
    public Stopwatch TimeSinceLastDmg { get; private set; }

    public bool Active
    {
        get
        {
            return _active;
        }
        set
        {
            _active = value;
            if (!_active)
            {
                DamagePerSecond = 0;
                Length = 0;
                TimeSinceLastDmg = null;
            }
            else
            {
                TimeSinceLastDmg = new Stopwatch();
                TimeSinceLastDmg.Start();
            }
        }
    }
    public int DamagePerSecond { get; private set; }
    public float Length { get; private set; }

    public float TotalDamage => DamagePerSecond * Length;
    // any other params

    private bool _active = false;

    public DoTInfo()
    {
        Active = false;
        DamagePerSecond = 0;
        Length = 0;
    }

    public void Set(int damagePerSecond, float length)
    {
        Active = true;
        DamagePerSecond = damagePerSecond;
        Length = length;
    }

}

internal class Damageable : Component
{
    public bool Hit { get; private set; }

    public int Health { get; set; } = 100;
    public readonly DoTInfo DoT = new DoTInfo();


    private int _hitTicks;

    public override void Start()
    {
        Health = 100;
    }

    public override void FixedUpdate()
    {
        if (Parent.HasComponent<RigidBody>() && Parent.RigidBody.IsStatic)
        {
            // entity is invulnerable
            Hit = false;
            _hitTicks = 0;
            Parent.SetColor(Color.White);

            return;
        }

        if (DoT.Active && (DoT.TimeSinceLastDmg.ElapsedMilliseconds >= 1000))
        {
            Damage(10, Vector2.Zero);
            DoT.TimeSinceLastDmg.Reset();
        }

        if (!Hit) return;

        _hitTicks++;
        if (_hitTicks > 15)
        {
            Hit = false;
            _hitTicks = 0;
            Parent.SetColor(Color.White);
        }
        else
        {
            Parent.SetColor(_hitTicks <= 15 / 2 ? Color.Red : Color.White);
        }
    }

    public void Damage(int damage, Vector2 normal)
    {
        if (Hit)
            return;

        if (Parent.HasComponent<RigidBody>() && Parent.RigidBody.IsStatic) return;

        Health -= damage;
        Hit = true;
        _hitTicks = 0;
        if (Health <= 0)
        {
            Parent.Destroy();
            DoT.Active = false;
            if (Parent.HasComponent<Boss>())
            {
                GameManager.Win();
            }
        }
        else
        {
            // knock back
            var rigidBody = Parent.RigidBody;
            rigidBody.Knockback(RigidBody.GetDirectionFromVector(normal),
                Physics.FixedDeltaTime * 5f * Physics.PlayerSpeed * Physics.PlayerMass);
        }
    }
}
