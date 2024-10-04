using System;
using System.Collections.Generic;
using System.Text;

internal class ButtonGrid : UIComponent
{
    private readonly Func<List<Button>> _buttonFunc;

    private List<Button> _buttons = new List<Button>();

    private int _ticker = 0;

    public ButtonGrid(Vector2 position, Vector2 size, Func<List<Button>> buttonFunc) : base(position, size, Color.White)
    {
        _buttonFunc = buttonFunc;

        UpdateButtonList();
    }

    public void UpdateButtonList()
    {
        foreach (var button in _buttons)
        {
            ParentScreen.Remove(button);
        }

        _buttons = _buttonFunc.Invoke();
    }

    public override void Draw()
    {
        if (_ticker++ % 60 == 0)
        {
            UpdateButtonList();
            _ticker = 0;
        }

        var x = Position.X;
        var y = Position.Y;
        foreach (var button in _buttons)
        {
            button.Position = new Vector2(x, y);

            x = button.Bounds.Max.X + 150;

            if (x > Size.X)
            {
                x = Position.X;
                y += button.Size.Y + 20;
            }

            if (ParentScreen.Contains(button)) continue;

            ParentScreen.Add(button);
        }
    }
}