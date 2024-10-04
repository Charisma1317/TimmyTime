using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using Newtonsoft.Json;


internal class SpriteRenderer : RendererComponent
{
    public const int DefaultFrameRate = 6;
    public static float TimePerFrame => 1000f / DefaultFrameRate;

    public static SpriteSheet DefaultSpriteSheet;

    private readonly List<Action> _drawActions = new List<Action>();

    public Vector2 Offset { get; set; } = Vector2.Zero;
    public Vector2 Scale { get; set; } = new Vector2(1, 1);

    public Sprite[] Frames { get; set; } = Array.Empty<Sprite>();
    
    public int CurrentFrame { get; set; } = 0;

    public bool Animate { get; set; } = true;

    public long Order { get; set; }

    public Color Color { get; set; } = Color.White;

    // default texture = white square
    public Bounds2 Bounds => Transform.Bounds;

    private int _frameCounter = 0;

    public override void Start()
    {
        if (Frames.Length == 0)
        {
            Frames = new[]
            {
                new Sprite
                {
                    SpriteSheet = DefaultSpriteSheet,
                    Bounds = new Bounds2(0, 0, 32, 32),
                    FrameContainer = DefaultSpriteSheet.Frames[0]
                }
            };
        }

        Order = Parent.Id;

        // update box collider if exists to match pivot offset
        if (Parent.HasComponent<BoxCollider>())
        {
            var collider = Parent.GetComponent<BoxCollider>();
            if (!collider.IsTrigger)
            {
                var frame = Frames[CurrentFrame];

                var pivot = frame.FrameContainer.Pivot;
                var source = frame.FrameContainer.SourceSize;

                collider.Offset = new Vector2(pivot.X * source.Width, 0);
            }
        }
    }

    public override void FixedUpdate()
    {
        if (Frames.Length == 0) return;

        if (!Animate)
        {
            CurrentFrame = 0; 
            return;
        }

        _frameCounter++;
        if (_frameCounter < DefaultFrameRate) return;

        _frameCounter = 0;
        CurrentFrame++;

        if (CurrentFrame >= Frames.Length) CurrentFrame = 0;
    }

    public void Draw(Action action)
    {
        _drawActions.Add(action);
    }

    public override void RenderAt(Vector2 p)
    {
        if (Frames.Length == 0) return;

        var pos = p + Offset;

        var frame = Frames[CurrentFrame];

        var sizeScaled = Bounds.Size * Scale;

        if (Parent.HasComponent<RigidBody>() && Parent.HasComponent<MovingEntity>())
        {
            // add direction indicator
            var rigidBody = Parent.GetComponent<RigidBody>();
            var left = rigidBody.Left;

            if (left)
                Engine.DrawTexture(frame.SpriteSheet.Texture, pos, Color,
                    mirror: TextureMirror.Horizontal, size: sizeScaled, source: frame.Bounds, scaleMode: TextureScaleMode.Nearest);
            else
                Engine.DrawTexture(frame.SpriteSheet.Texture, pos, Color, size: sizeScaled, source: frame.Bounds, scaleMode: TextureScaleMode.Nearest);
        }
        else
        {
            Engine.DrawTexture(frame.SpriteSheet.Texture, pos, Color, size: sizeScaled, source: frame.Bounds, scaleMode: TextureScaleMode.Nearest);
        }

        foreach (var acc in _drawActions) acc.Invoke();
        _drawActions.Clear();
    }

    public override void Render()
    {
        RenderAt(Transform.Position);
    }
}

class Sprite
{
    public SpriteSheet SpriteSheet { get; set; }
    public Bounds2? Bounds { get; set; } = null;

    public SpriteSheet.FrameContainer FrameContainer { get; set; }
}

class SpriteSheet
{
    [JsonIgnore] public Texture Texture { get; set; }

    [JsonProperty("frames")] public FrameContainer[] Frames { get; set; }

    [JsonProperty("meta")] public MetaData Meta { get; set; }

    public class FrameContainer
    {
        [JsonProperty("filename")] public string FileName { get; set; }

        [JsonProperty("frame")] public FrameData Frame { get; set; }

        [JsonProperty("rotated")] public bool Rotated { get; set; }

        [JsonProperty("trimmed")] public bool Trimmed { get; set; }

        [JsonProperty("spriteSourceSize")] public FrameData SpriteSourceSize { get; set; }

        [JsonProperty("sourceSize")] public FrameData SourceSize { get; set; }

        [JsonProperty("pivot")] public Vector2 Pivot { get; set; }

        public class FrameData
        {
            [JsonProperty("x")] public int X { get; set; }

            [JsonProperty("y")] public int Y { get; set; }

            [JsonProperty("w")] public int Width { get; set; }

            [JsonProperty("h")] public int Height { get; set; }

            public Bounds2 ToBounds()
            {
                return new Bounds2(X, Y, Width, Height);
            }
        }
    }

    public class MetaData
    {
        [JsonProperty("app")] public string App { get; set; }

        [JsonProperty("version")] public string Version { get; set; }

        [JsonProperty("image")] public string Image { get; set; }

        [JsonProperty("format")] public string Format { get; set; }

        [JsonProperty("size")] public SizeData Size { get; set; }

        [JsonProperty("scale")] public string Scale { get; set; }

        [JsonProperty("smartupdate")] public string Smartupdate { get; set; }

        public class SizeData
        {
            [JsonProperty("w")] public int Width { get; set; }

            [JsonProperty("h")] public int Height { get; set; }
        }
    }
}