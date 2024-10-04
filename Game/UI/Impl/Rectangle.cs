using System;
using System.Collections.Generic;
using System.Text;

internal class Rectangle : UIComponent
{
    public bool Filled { get; set; } = true;
    public Rectangle(Vector2 position, Vector2 size, bool filled, Color color) : base(position, size, color)
    {
        Filled = filled;
    }

    public override void Draw()
    {
        if (Filled) Engine.DrawRectSolid(new Bounds2(Position, Size), Color);
        else
            Engine.DrawRectEmpty(new Bounds2(Position, Size), Color);
    }

    // float opacity = 0.0f - 1.0f
    public void SetOpacity(float opacity)
    {
        Color = new Color(Color.R, Color.G, Color.B, (byte)(255 * opacity));
    }
}
