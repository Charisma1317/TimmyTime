using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

internal class Bow : Component
{
    public struct Target
    {
        private Vector2 _position;

        public GameObject GameObject { get; set; }

        public Vector2 Position
        {
            get => GameObject != null ? GameObject.Transform.Center : _position;
            set => _position = value;
        }

        public Target(float x, float y)
        {
            _position = new Vector2(x, y);
            GameObject = null;
        }

        public Target(GameObject gameObject)
        {
            GameObject = gameObject;
            _position = Vector2.Zero;
        }
    }

    public readonly float PullStageTime = 0.25f; // 1 stage / 0.25 seconds
    public readonly int PullStages = 3;

    public double PullTime => PullStageTime * PullStages; // in seconds


    public int PullStage { get; set; } = 0;

    public Arrow Arrow { get; set; }

    public Target ArrowTarget { get; set; } 

    private int _lockedEnemyIndex = -1;

    private float _pullTimer = 0f;

    public SpriteRenderer BowRenderer { get; set; }

    public override void Start()
    {
        // add another sprite renderer for the bow
        if (BowRenderer != null) return;

        BowRenderer = Parent.AddComponent<SpriteRenderer>();
        BowRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "bow");
        BowRenderer.CurrentFrame = 0; // default to 0
        BowRenderer.Scale = new Vector2(0.75f, 0.5f); // 12, 32 -> 9, 16 == 0.75, 0.5
    }

    public void Pull()
    {
        // increase pull stage by 1 every 0.5 seconds
        // 3 stages total = 1.5 seconds to max pull
        _pullTimer += Engine.TimeDelta;
        if (!(_pullTimer >= PullStageTime)) return;

        _pullTimer = 0f;
        PullStage++;

        if (PullStage > 0 && Arrow == null)
        {
            // generate arrow
            var pos = Parent.Transform.Position + BowRenderer.Offset + new Vector2(0, 4);
            
            var obj = (GameObject)PrefabManager.Prefabs["Arrow"].Invoke(null, new object[] { pos });

            Arrow = obj.GetComponent<Arrow>();

            GameManager.Load(obj);
        }

        // clamp pull stage to 3
        if (PullStage > PullStages)
        {
            PullStage = PullStages;
        }

        if (_lockedEnemyIndex != -1) return;

        // recalculate arrow end position
        // stage 1 arrow x vel = 100
        // stage 2 arrow x vel = 200
        // stage 3 arrow x vel = 300
        var arrowXVel = 100 * PullStage;

        var endX = Parent.Transform.Position.X + (Parent.RigidBody.Left ? -arrowXVel : arrowXVel);

        // calculate arrow end position y
        // let dy = 9.8 * m * (1/60.0f)
        // if dy is the position change per frame on the y axis for an arrow
        // then we can calculate the end y by doing
        var endY = Parent.Transform.Position.Y;

        ArrowTarget = new Target(endX, endY);
    }

    public void Shoot()
    {
        // move arrow in front of the player
        var pos = Parent.Transform.Position + BowRenderer.Offset + new Vector2(Parent.BoxCollider.Size.X, 0);
        if (Parent.RigidBody.Left)
            pos = Parent.Transform.Position + BowRenderer.Offset - new Vector2(Parent.BoxCollider.Size.X, 0);
        Arrow.Parent.RigidBody.Position = pos;
        Arrow.Parent.RigidBody.IsStatic = false;
        Arrow.Parent.BoxCollider.IsTrigger = false;

        Arrow.EndPosition = ArrowTarget.Position;
        Arrow.Power = PullStage;

        _lockedEnemyIndex = -1;
        PullStage = 0;
        ArrowTarget = new Target();
        Arrow = null;
    }

    private List<GameObject> GetClosestEnemies()
    {
        var enemies = GameManager.LoadedObjectsByLabel[Label.Enemy];
        var o = new List<GameObject>();

        var cam = GameManager.MainCamera.GetComponent<Camera>();

        // return a list of enemies that are the closest whilst being on screen
        foreach (var enemy in enemies)
        {
            if (!cam.RenderedObjects.Contains(enemy)) continue;
            o.Add(enemy);
        }

        // filter out enemies on roughly the same y axis as other enemies
        // this is to prevent the player from locking onto enemies that are on the same y axis
        // as other enemies, as this would cause the player to lock onto the wrong enemy
        
        // sort by y axis
        o.Sort((a, b) => a.Transform.Position.Y.CompareTo(b.Transform.Position.Y));

        // filter out enemies that are on the same y axis as other enemies
        var filtered = new List<GameObject>();
        var lastY = float.MinValue;
        foreach (var enemy in o)
        {
            if (Math.Abs(enemy.Transform.Position.Y - lastY) < 10f) continue;
            filtered.Add(enemy);
            lastY = enemy.Transform.Position.Y;
        }

        o = filtered;

        return o;
    }

    public override void Update()
    {
        var player = Parent.GetComponent<Player>();
        if (player.EquippedWeaponType != WeaponType.Bow) return;

        if (Parent.RigidBody.Left)
            BowRenderer.Offset = new Vector2(-6, 9);
        else
            BowRenderer.Offset = new Vector2(9, 9);

        if (Arrow != null)
        {
            Arrow.Parent.RigidBody.Left = Parent.RigidBody.Left;
            var pos = Parent.Transform.Position + BowRenderer.Offset + new Vector2(0, 4);
            Arrow.Parent.RigidBody.Position = pos;
        }

        // draw locked target if there is one
        if (ArrowTarget.Position != Vector2.Zero)
        {
            BowRenderer.Draw(() =>
            {
                var camPos = GameManager.MainCamera.Transform.Position;
                Engine.DrawRectSolid(new Bounds2(ArrowTarget.Position.X - 5 - camPos.X, ArrowTarget.Position.Y - 5 - camPos.Y, 5, 5), Color.White);
            });
        }

        // update bow sprite
        BowRenderer.CurrentFrame = PullStage;

        if (Engine.GetKeyDown(Key.Q) || Engine.GetKeyHeld(Key.Q))
        {
            Pull();
        }

        // arrow keys for lock on to enemies
        if (Engine.GetKeyDown(Key.Up) || Engine.GetKeyDown(Key.Down))
        {
            _lockedEnemyIndex += Engine.GetKeyDown(Key.Up) ? 1 : -1;
            // get all enemies
            var enemies = GetClosestEnemies();

            // clamp index
            if (_lockedEnemyIndex < 0) _lockedEnemyIndex = enemies.Count - 1;
            if (_lockedEnemyIndex > enemies.Count - 1) _lockedEnemyIndex = 0;

            if (enemies.Count == 0) return;

            var closestEnemy = enemies[_lockedEnemyIndex];

            if (closestEnemy != null)
            {
                // calculate arrow end position
                ArrowTarget = new Target(closestEnemy);
            }
        }


        if (Engine.GetKeyUp(Key.Q) && Arrow != null)
        {
            Shoot();
        }
    }
    
    public override void FixedUpdate()
    {

    }
}
