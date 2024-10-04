using System;

internal class Text : UIComponent
{
    public Text(TextProperties? props, Vector2 pos, Color bgColor) : base(
        pos, Vector2.Zero, bgColor)
    {
        Properties = props;

        if (props.HasValue)
            Size = GetTextSize(props.Value.Text) + Padding;
    }

    public TextProperties? Properties { get; set; }

    private Vector2 Padding
    {
        get
        {
            if (!Properties.HasValue)
                return new Vector2(20, 20);

            var font = Properties.Value.Font;
            if (font == Game.BigFont)
                return new Vector2(20, 20);
            if (font == Game.MediumFont)
                return new Vector2(30, 20);
            if (font == Game.SmallFont)
                return new Vector2(40, 20);

            return new Vector2(20, 20);
        }
    }

    public override Bounds2 Bounds => new Bounds2(Position - new Vector2(Size.X / 2, Padding.Y / 2), Size);

    public override void Draw()
    {
        if (!Properties.HasValue)
            return;

        var props = Properties.Value;

        Size = GetTextSize(props.Text) + Padding;

        // draw the start and end of the text

        Engine.DrawRectSolid(Bounds, Color);
        Engine.DrawString(props.Text, GetPositionFromAlignment(), props.TextColor, props.Font,
            props.Alignment);
    }

    protected Vector2 GetTextSize(string text)
    {
        if (!Properties.HasValue)
            return Vector2.Zero;

        var props = Properties.Value;

        return Engine.DrawString(text, Position, props.TextColor, props.Font, props.Alignment, true).Size;
    }

    protected Vector2 GetPositionFromAlignment()
    {
        if (!Properties.HasValue)
            return Vector2.Zero;

        var props = Properties.Value;
        var alignment = props.Alignment;
        Vector2 pos;

        var size = GetTextSize(props.Text);

        var y = Position.Y + size.Y / 10;

        switch (alignment)
        {
            case TextAlignment.Left:
                // center on y axis, left + (small padding relative to the width of the button)
                pos = new Vector2(Position.X, y);
                break;
            case TextAlignment.Center:
                // center on y axis, center on x axis
                pos = new Vector2(Position.X, y);
                break;
            case TextAlignment.Right:
                // center on y axis, right - (small padding relative to the width of the button)
                var rightX = Position.X + size.X;
                pos = new Vector2(rightX - size.X / 10, y);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // pos = Position;

        return pos;
    }

    public static Text TitleText(string text, Color textColor, Color bgColor, Vector2 pos)
    {
        var props = new TextProperties(text, Game.BigFont, textColor, TextAlignment.Center);

        return new Text(props, pos, bgColor);
    }

    public static Text InfoText(string text, Color textColor, Color bgColor, Vector2 pos)
    {
        var props = new TextProperties(text, Game.MediumFont, textColor, TextAlignment.Center);
        return new Text(props, pos, bgColor);
    }

    public static Text SmallerInfoText(string text, Color textColor, Color bgColor, Vector2 pos)
    {
        var props = new TextProperties(text, Game.SmallFont, textColor, TextAlignment.Center);
        return new Text(props, pos, bgColor);
    }


    public struct TextProperties
    {
        public string Text { get; set; }
        public Font Font { get; set; }
        public Color TextColor { get; set; }
        public TextAlignment Alignment { get; set; }

        public TextProperties(string text, Font font, Color textColor, TextAlignment alignment)
        {
            Text = text;
            Font = font;
            TextColor = textColor;
            Alignment = alignment;
        }
    }
}