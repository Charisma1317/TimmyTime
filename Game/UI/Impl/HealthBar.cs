using System;
using System.Collections.Generic;
using System.Text;

class HealthBar : UIComponent
{
    private readonly Vector2 _pos;
    private Vector2 _currHealth;
    

    public HealthBar(Vector2 pos, Vector2 health) : base(pos, new Vector2(200, 20), Color.Black)
    {
        _pos = pos;
        _currHealth = health;

    }

    public void UpdateHealth()
    {
        _currHealth.X = Game.MainPlayer.GetComponent<Damageable>().Health * 2;
    }

    public override void Draw()
    {
        UpdateHealth();
        Engine.DrawRectSolid(new Bounds2(_pos, new Vector2(200, 20)), Color.Black);
        Engine.DrawRectSolid(new Bounds2(_pos, _currHealth), Color.Red);
    }
}
