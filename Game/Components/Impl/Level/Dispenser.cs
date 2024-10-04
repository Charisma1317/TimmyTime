using System;
using System.Collections.Generic;
using System.Text;
internal class Dispenser : Component
{
    public bool Right { get; set; } = false;
    public int timer;
    public Arrow Arrow { get; set; }

    public override void FixedUpdate()
    {
        if (Parent.SpriteRenderer.CurrentFrame == 0)
        {
            Parent.SpriteRenderer.Animate = false;
        }

        timer += 1;
        if (timer % 100 == 0)
        {
            Console.WriteLine("Fired");
            Fire();
            Parent.SpriteRenderer.Animate = true;
            Parent.SpriteRenderer.CurrentFrame = 1;
        }
    }
    
    public void CreateArrow()
    {
        var pos = Parent.Transform.Position + new Vector2(0, 15);

        var obj = (GameObject)PrefabManager.Prefabs["Arrow"].Invoke(null, new object[] { pos });

        Arrow = obj.GetComponent<Arrow>();
        Arrow.SetPoisonous();

        GameManager.Load(obj);
    }
    public void Fire
        ()
    {
        var level = LevelManager.CurrentLevel;
        CreateArrow();
        //Error Here
        var pos = Parent.Transform.Position + new Vector2(Parent.Transform.Size.X, 0);
        if (Parent.RigidBody.Left)
            pos = Parent.Transform.Position - new Vector2(Parent.Transform.Size.X, 0);
        //End Here
        Arrow.FirstUpdate = false;
        Arrow.Parent.GetComponent<RigidBody>().Mass = 16f;
        Arrow.Parent.RigidBody.Position = pos;
        Arrow.Parent.RigidBody.IsStatic = false;
        Arrow.Parent.BoxCollider.IsTrigger = false;

        Arrow.EndPosition = new Vector2(Parent.RigidBody.Position.X - 200, Parent.RigidBody.Position.Y + 15);
        if (Right) Arrow.EndPosition = new Vector2(Parent.RigidBody.Position.X + 200, Parent.RigidBody.Position.Y + 15);
        Arrow.Power = 3;

        /*var obj = (GameObject)PrefabManager.Prefabs["Arrow"].Invoke(null, new object[] { Vector2.Zero });

        // right side of player
        var defaultPos = new Vector2(Parent.RigidBody.Position.X + obj.Transform.Size.X + 15f,
            Parent.RigidBody.Position.Y + (obj.Transform.Size.Y / 2));

        // check if the player is facing left
        if (!Right)
            defaultPos = new Vector2(Parent.RigidBody.Position.X - obj.Transform.Size.X - 5f,
                Parent.RigidBody.Position.Y);

        obj.Transform.Position = defaultPos;

        var arrowBody = obj.GetComponent<RigidBody>();
        arrowBody.Mass = Physics.PlayerMass / 2;
        arrowBody.Velocity =
            new Vector2(
                Right ? Physics.PlayerSpeed * .75f : -Physics.PlayerSpeed * .75f, 0);

        var arrow = obj.GetComponent<Arrow>();
        GameManager.Load(obj);*/
    }
}
