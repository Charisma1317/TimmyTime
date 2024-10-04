using System;

internal class ProgressBar : UIComponent
{
    private float _progress;

    public ProgressBar(Vector2 position, Vector2 size, Color color) : base(position, size, color)
    {
    }

    public float Progress { get; set; } = 0f; // 0.0f % -> 100.0f %

    public override void Draw()
    {
        if (Math.Abs(Progress - _progress) <= 0.01f)
            _progress = Vector2.Lerp(_progress, Progress, Engine.TimeDelta * 5f);

        var progressBounds = new Bounds2(Position, new Vector2(Size.X * _progress, Size.Y));
        Engine.DrawRectSolid(progressBounds, Color); // progress bar
        Engine.DrawRectEmpty(Bounds, Color.Black); // outline
    }
}