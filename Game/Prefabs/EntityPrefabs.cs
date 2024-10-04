using System;
using System.Numerics;
using System.Security.Principal;

internal static class EntityPrefabs
{
    [Prefab(typeof(Player))]
    public static GameObject CreatePlayer(Vector2 position)
    {
        var player = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(12, 32)
            }
        };

        player.AddComponent<SpriteRenderer>();
        player.AddComponent<RigidBody>();
        player.AddComponent<BoxCollider>();
        player.AddComponent<MovingEntity>();

        player.AddComponent<Damageable>();
        player.AddComponent<Player>();

        player.AddComponent<Bow>();

        var spriteRenderer = player.GetComponent<SpriteRenderer>();
        spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "main");

        var boxCollider = player.GetComponent<BoxCollider>();
        boxCollider.Size = new Vector2(12, 32);

        player.Label = Label.Player;

        return player;
    }

    [Prefab(typeof(Arrow))]
    public static GameObject CreateArrow(Vector2 position)
    {
        var arrow = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(12, 7)
            }
        };

        arrow.Label = Label.Projectile;

        arrow.AddComponent<SpriteRenderer>();
        arrow.AddComponent<RigidBody>();
        arrow.AddComponent<Arrow>();

        var b = arrow.AddComponent<BoxCollider>();
        b.Size = new Vector2(16, 7);

        var spriteRenderer = arrow.GetComponent<SpriteRenderer>();
        spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "stone-arrow");

        return arrow;
    }

    [Prefab(typeof(SimpleMovingEnemy))]
    public static GameObject CreateSimpleMovingEnemy(Vector2 position, MovingBounds movingBounds)
    {
        var enemy = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(16, 16)
            }
        };

        enemy.AddComponent<SpriteRenderer>();
        enemy.AddComponent<RigidBody>();
        enemy.AddComponent<BoxCollider>();
        enemy.AddComponent<MovingEntity>();

        enemy.AddComponent<Damageable>();
        enemy.AddComponent<SimpleMovingEnemy>();

        var simpleMovingEnemy = enemy.GetComponent<SimpleMovingEnemy>();
        simpleMovingEnemy.MovingBounds = movingBounds;

        var spriteRenderer = enemy.GetComponent<SpriteRenderer>();
        spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "spritesheet-10");

        enemy.Label = Label.Enemy;

        return enemy;
    }

    [Prefab(typeof(Conveyer), overrideFree: true)]
    public static GameObject CreateConveyer(Vector2 position, int length, bool right)
    {
        var conveyer = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(length * 32, 32)
            }
        };

        conveyer.AddComponent<SpriteRenderer>();
        conveyer.AddComponent<RigidBody>();
        conveyer.AddComponent<BoxCollider>();
        conveyer.AddComponent<Conveyer>();
        conveyer.GetComponent<Conveyer>().Right = right;

        var rigidBody = conveyer.GetComponent<RigidBody>();
        rigidBody.IsStatic = true;

        var spriteRenderer = conveyer.GetComponent<SpriteRenderer>();
        spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "spritesheet-5");

        return conveyer;
    }

    [Prefab(typeof(AIEntity))]
    public static GameObject CreateAIEntity(Vector2 position)
    {
        var aiEntity = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(16, 16)
            }
        };

        aiEntity.AddComponent<SpriteRenderer>();

        aiEntity.AddComponent<RigidBody>();
        aiEntity.AddComponent<BoxCollider>();
        aiEntity.AddComponent<MovingEntity>();

        aiEntity.AddComponent<Damageable>();
        aiEntity.AddComponent<AIEntity>();

        var spriteRenderer = aiEntity.GetComponent<SpriteRenderer>();
        spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "spritesheet-0");

        aiEntity.Label = Label.Enemy;

        return aiEntity;
    }

    [Prefab(typeof(MovingPlatform))]
    public static GameObject CreateMovingPlatform(Vector2 position, int length, MovingBounds movingBounds)
    {
        var platform = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(32 * length, 32)
            }
        };
        platform.AddComponent<SpriteRenderer>();

        platform.AddComponent<RigidBody>();
        platform.AddComponent<BoxCollider>();
        platform.AddComponent<MovingEntity>();
        platform.AddComponent<MovingPlatform>();

        var spriteRenderer = platform.GetComponent<SpriteRenderer>();

        if (movingBounds != MovingBounds.Default)
        {
            platform.GetComponent<MovingPlatform>().MovingBounds = movingBounds;
            spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "spritesheet-8");
        }
        else
        {
            platform.GetComponent<MovingPlatform>().Moving = false;
            spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "spritesheet-6");
        }

        return platform;
    }

    [Prefab(typeof(Boss))]
    public static GameObject CreateBoss(Vector2 position)
    {
        var boss = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(32, 32)
            }
        };
        boss.AddComponent<SpriteRenderer>();

        boss.AddComponent<RigidBody>();
        boss.AddComponent<BoxCollider>();
        boss.AddComponent<MovingEntity>();

        var dmg = boss.AddComponent<Damageable>();
        boss.AddComponent<Boss>();

        dmg.Health = 500;

        var spriteRenderer = boss.GetComponent<SpriteRenderer>();
        spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "spritesheet-4");

        boss.Label = Label.Enemy;

        return boss;
    }

    [Prefab(typeof(BossBaby))]
    public static GameObject CreateBossBaby(Vector2 position)
    {
        var boss = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(21, 18)
            }
        };
        boss.AddComponent<SpriteRenderer>();

        boss.AddComponent<RigidBody>();
        boss.AddComponent<BoxCollider>();
        boss.AddComponent<MovingEntity>();

        boss.AddComponent<Damageable>();
        boss.AddComponent<BossBaby>();

        var spriteRenderer = boss.GetComponent<SpriteRenderer>();
        spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "spritesheet-11");

        boss.Label = Label.Enemy;

        return boss;
    }
}