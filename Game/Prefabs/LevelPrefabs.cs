internal static class LevelPrefabs
{
    [Prefab(typeof(Block))]
    public static GameObject CreateBlock(Vector2 position)
    {
        var block = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(32, 32)
            }
        };

        block.AddComponent<SpriteRenderer>();

        block.AddComponent<RigidBody>();
        block.AddComponent<Block>();

        var rigidBody = block.GetComponent<RigidBody>();
        rigidBody.IsStatic = true;

        var spriteRenderer = block.GetComponent<SpriteRenderer>();
        spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "spritesheet-3");

        return block;
    }

    [Prefab(typeof(Ladder))]
    public static GameObject CreateLadder(Vector2 position)
    {
        var ladder = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(32, 32)
            }
        };

        ladder.AddComponent<SpriteRenderer>();

        ladder.AddComponent<RigidBody>();
        ladder.AddComponent<BoxCollider>();
        ladder.AddComponent<Ladder>();

        var rigidBody = ladder.GetComponent<RigidBody>();
        rigidBody.IsStatic = true;

        var spriteRenderer = ladder.GetComponent<SpriteRenderer>();
        spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "spritesheet-2");

        return ladder;
    }

    [Prefab(typeof(WinTrigger))]
    public static GameObject CreateWinTrigger(Vector2 position)
    {
        var winTrigger = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(32, 32)
            }
        };

        winTrigger.AddComponent<SpriteRenderer>();
        winTrigger.AddComponent<RigidBody>();
        winTrigger.AddComponent<MovingEntity>();
        winTrigger.AddComponent<WinTrigger>();

        var rigidBody = winTrigger.GetComponent<RigidBody>();
        rigidBody.IsStatic = true;

        var spriteRenderer = winTrigger.GetComponent<SpriteRenderer>();
        spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "spritesheet-7");

        return winTrigger;
    }

    [Prefab(typeof(Powerup))]
    public static GameObject Powerup(Vector2 position)
    {
        var powerup = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(32, 32)
            }
        };

        powerup.AddComponent<SpriteRenderer>();
        powerup.AddComponent<RigidBody>();
        powerup.AddComponent<MovingEntity>();
        powerup.AddComponent<Powerup>();

        var rigidBody = powerup.GetComponent<RigidBody>();
        rigidBody.IsStatic = true;

        var spriteRenderer = powerup.GetComponent<SpriteRenderer>();
        spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "spritesheet-13");

        return powerup;
    }

    [Prefab(typeof(Dispenser))]
    public static GameObject CreateDispenser(Vector2 position, Direction dir)
    {
        var Dispenser = new GameObject
        {
            Transform =
            {
                Position = position,
                Size = new Vector2(32, 32),
                
            }
        };

        Dispenser.AddComponent<SpriteRenderer>();
        Dispenser.AddComponent<RigidBody>();
        Dispenser.AddComponent<Dispenser>();

        var rigidBody = Dispenser.GetComponent<RigidBody>();
        rigidBody.IsStatic = true;

        var spriteRenderer = Dispenser.GetComponent<SpriteRenderer>();
        spriteRenderer.Frames = Game.GetFrames(SpriteRenderer.DefaultSpriteSheet, "Dispenser-");

        return Dispenser;
    }
}