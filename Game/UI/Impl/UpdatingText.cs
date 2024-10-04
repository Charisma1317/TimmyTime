using System;

internal class UpdatingText : Text
{
    private readonly Font _font;
    private Func<string> _result;
    private readonly Color _textColor;

    public UpdatingText(Func<string> result, Color textColor, Font font, Vector2 pos) : base(null,
        pos, Color.Transparent)
    {
        _result = result;
        _textColor = textColor;
        _font = font;
        Properties = new TextProperties(result.Invoke(), font, textColor, TextAlignment.Center);
    }

    public string Text
    {
        get => _result.Invoke();
        set => _result = () => value;
    }

    public override void Draw()
    {
        Properties = new TextProperties(Text, _font, _textColor, TextAlignment.Center);
        Size = GetTextSize(Properties.Value.Text);

        base.Draw();
    }

    public static UpdatingText TitleText(Func<string> result, Color textColor, Vector2 pos)
    {
        return new UpdatingText(result, textColor, Game.BigFont, pos);
    }

    public new static UpdatingText InfoText(Func<string> result, Color textColor, Vector2 pos)
    {
        return new UpdatingText(result, textColor, Game.MediumFont, pos);
    }

    public static UpdatingText DebugText(Func<string> result, Color textColor, Vector2 pos)
    {
        return new UpdatingText(result, textColor, Game.SmallFont, pos);
    }
}