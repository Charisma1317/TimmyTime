using System;
using System.Collections.Generic;
using System.Linq;
using SDL2;

internal class Camera : RendererComponent
{
    private static Texture background = Engine.LoadTexture("parallax_test_img.jpg");

    protected const int CameraBorder = 1;
    protected static readonly int CameraRenderBuffer = 100;

    protected IntPtr Renderer;
    protected Texture Target;

    protected int TargetHeight;
    protected int TargetWidth;

    public Bounds2 BufferedBounds => new Bounds2(Transform.Position,
        new Vector2(Transform.Size.X + CameraRenderBuffer, Transform.Size.Y + CameraRenderBuffer));

    public List<GameObject> RenderedObjects => GameManager.GetObjectsWithinBounds(BufferedBounds);

    private Texture CreateTexture()
    {
        var size = Transform.Size;
        var format = SDL.SDL_PIXELFORMAT_RGBA8888;

        TargetWidth = (int)Math.Ceiling(size.X + 2 * CameraBorder);
        TargetHeight = (int)Math.Ceiling(size.Y + 2 * CameraBorder);

        var a = SDL.SDL_CreateTexture(Renderer, format,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET,
            TargetWidth, TargetHeight);

        SDL.SDL_SetTextureBlendMode(a, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

        var texture = new Texture(a, (int)Game.Resolution.X, (int)Game.Resolution.Y);

        return texture;
    }

    public override void Start()
    {
        Renderer = Engine.Renderer;
        Target = CreateTexture();

        var center = Transform.Center;
        center.Y = 260;
        Transform.Center = center;
    }

    public override void Update()
    {
        if (!Parent.Equals(GameManager.MainCamera)) return;
        var mainPlayer = Game.MainPlayer;

        var center = Transform.Center;
        var dest = mainPlayer.Transform.Center;

        if (Math.Abs(center.X - dest.X) <= 1f) return;

        center.X = Vector2.Lerp(Transform.Center.X, Game.MainPlayer.Transform.Center.X, Engine.TimeDelta * 5f);
        Transform.Center = center;
    }

    public override void Render()
    {
        // set render target
        SDL.SDL_SetRenderTarget(Renderer, Target.Handle);
        SDL.SDL_SetRenderDrawColor(Renderer, 0, 0, 0, 0xFF);
        SDL.SDL_RenderClear(Renderer);

        //Engine.DrawRectSolid(new Bounds2(Vector2.Zero, Transform.Size), Color.LightCoral);

        // render all entities
        var objects = RenderedObjects.OrderByDescending(obj => obj.Id).ToList();
        objects.Add(LevelManager.CurrentLevel);

        // draw level uniform grid
        var level = LevelManager.CurrentLevel;
        var grid = level.UniformGrid;
        var gridWidth = grid.GetLength(0);
        var gridHeight = grid.GetLength(1);
        var tileSizeX = 64;
        var tileSizeY = 64;

        var localPosBackground = new Vector2(-250 - Transform.Position.X / 2,
                   0 - Transform.Position.Y / 2 - CameraBorder);
        var transparentCol = Color.White.WithAlpha(0.75f);

        Engine.DrawTexture(background, localPosBackground, transparentCol, new Vector2(1920, 540));


        // draw grid
        for (var i = 0; i < gridWidth; i++)
            for (var j = 0; j < gridHeight; j++)
            {
                var tile = grid[i, j];
                if (tile == null) continue;

                var tilePos = new Vector2(i * tileSizeX, j * tileSizeY);
                var localPos = new Vector2(tilePos.X - Transform.Position.X / 2 + CameraBorder,
                    tilePos.Y - Transform.Position.Y + CameraBorder);

                var tileBounds = new Bounds2(localPos, new Vector2(tileSizeX, tileSizeY));

                Engine.DrawRectEmpty(tileBounds, Color.White.WithAlpha(0.1f));
            }

        foreach (var gameObj in objects)
        {
            if (gameObj.HasComponent<Camera>()) continue;

            var oldPos = gameObj.Transform.Position;
            var hasRigidBody = gameObj.HasComponent<RigidBody>();
            if (hasRigidBody) oldPos = gameObj.RigidBody.Position;

            // update entity pos to local pos
            var localPos = new Vector2(oldPos.X - Transform.Position.X + CameraBorder,
                oldPos.Y - Transform.Position.Y + CameraBorder);

            foreach (var renderer in gameObj.GetRendererComponents())
                renderer.RenderAt(localPos);

            // DrawColliderFrame(gameObj);
        }

        SDL.SDL_SetRenderTarget(Renderer, Engine.RenderTarget.Handle);
        SDL.SDL_SetRenderDrawColor(Renderer, 0, 0, 0, 255);
        SDL.SDL_RenderClear(Renderer);

        // render camera
        var pixelH = Game.Resolution.Y / Transform.Size.Y;
        var corX = (int)Transform.Position.X - Transform.Position.X;
        var corY = (int)Transform.Position.Y - Transform.Position.Y;

        var dest = new SDL.SDL_Rect
        {
            x = (int)(corX * pixelH - pixelH * CameraBorder),
            y = (int)(corY * pixelH - pixelH * CameraBorder),
            w = (int)(TargetWidth * pixelH),
            h = (int)(TargetHeight * pixelH)
        };

        SDL.SDL_RenderCopy(Renderer, Target.Handle, IntPtr.Zero, ref dest);
    }

    protected void DrawColliderFrame(GameObject gameObj)
    {
        if (gameObj.HasComponent<CompositeCollider>())
        {
            var colliders = gameObj.GetComponents<CompositeCollider>();

            foreach (var col in colliders)
            {
                var bounds = col.AABB();
                var localBounds = new Bounds2(bounds.Position - Transform.Position, bounds.Size);
                Engine.DrawRectEmpty(localBounds, Color.Blue);
            }
        }

        if (gameObj.HasComponent<BoxCollider>())
        {
            var colliders = gameObj.GetComponents<BoxCollider>();
            foreach (var col in colliders)
            {
                var bounds = col.AABB();
                var localBounds = new Bounds2(bounds.Position - Transform.Position, bounds.Size);

                if (col.IsTrigger)
                    Engine.DrawRectEmpty(localBounds, Color.Black);
                else Engine.DrawRectEmpty(localBounds, Color.MidnightBlue);
            }
        }
    }
}