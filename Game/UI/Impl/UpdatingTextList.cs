using System;
using System.Collections.Generic;

internal class UpdatingTextList : UIComponent
{
    private readonly Func<List<string>> _result;

    public UpdatingTextList(Func<List<string>> result, Vector2 position, Color textColor) : base(position, Vector2.Zero,
        textColor)
    {
        _result = result;
        Texts = result.Invoke();
    }

    public List<string> Texts { get; private set; }

    public void UpdateTexts()
    {
        Texts = _result.Invoke();
    }

    public override void Draw()
    {
        UpdateTexts();

        var y = Position.Y;
        foreach (var text in Texts)
        {
            var pos = new Vector2(Position.X, y);
            Engine.DrawString(text, pos, Color, Game.SmallFont);
            y += Game.SmallFont.PointSize;
        }
    }
}