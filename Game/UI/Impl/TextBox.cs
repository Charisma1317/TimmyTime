using System;
using System.Diagnostics;

internal class TextBox : Text
{
    private bool _focused;

    private readonly Func<string, bool> _onEnter;

    private char[] _text;
    private int _textCursor;

    public TextBox(Func<string, bool> onEnter, Vector2 pos, TextProperties props, Color bgColor, int maxChars) : base(
        props,
        pos, bgColor)
    {
        _onEnter = onEnter;
        Properties = props;
        MaxChars = maxChars;
        _text = new char[maxChars];

        Size = new Vector2(props.Font.PointSize * maxChars, props.Font.PointSize);
    }

    public string Text => new string(_text).Replace("\0", "_");

    private int MaxChars { get; }

    public override Bounds2 Bounds => new Bounds2(Position - new Vector2(Size.X / 2, 0), Size);

    public static float GetStringWidth(Font font, string str, TextAlignment alignment)
    {
        return GetStringSize(font, str, alignment).X;
    }

    public static float GetStringHeight(Font font, string str, TextAlignment alignment)
    {
        return GetStringSize(font, str, alignment).Y;
    }

    public static Vector2 GetStringSize(Font font, string str, TextAlignment alignment)
    {
        return Engine.DrawString(str, Vector2.Zero, Color.White, font, alignment, true).Size;
    }

    public static float GetCharWidth(Font font, char c, TextAlignment alignment)
    {
        return GetCharSize(font, c, alignment).X;
    }

    public static float GetCharHeight(Font font, char c, TextAlignment alignment)
    {
        return GetCharSize(font, c, alignment).Y;
    }

    public static Vector2 GetCharSize(Font font, char c, TextAlignment alignment)
    {
        return GetStringSize(font, c + "", alignment);
    }

    public override void Draw()
    {
        if (!Properties.HasValue) return;

        var props = Properties.Value;
        props.Text = Text;
        Size = new Vector2(GetCharWidth(props.Font, '_', props.Alignment) * MaxChars + 15 * MaxChars,
            props.Font.PointSize);

        Properties = props;

        Engine.DrawRectEmpty(Bounds, _focused ? Color.Green : Color.White);
        // draw every index from the text array with equal spacing such that it takes up the entire width of the Bounds box
        var boxWidth = Bounds.Size.X;
        for (var i = 0; i < MaxChars; i++)
        {
            var c = Text[i];
            var width = GetCharWidth(props.Font, c, props.Alignment) + 1f;

            var x = Bounds.Position.X + boxWidth / MaxChars * i + (boxWidth / MaxChars - width) / 2;
            var y = Bounds.Position.Y + (Bounds.Size.Y - GetCharHeight(props.Font, c, props.Alignment)) / 2;

            var bounds = Engine.DrawString(c + "", new Vector2(x, y), props.TextColor, props.Font, props.Alignment);
            // Engine.DrawRectEmpty(bounds, Color.Blue);
        }
    }

    public override void OnClick(ClickEvent e)
    {
        var prevFocused = _focused;
        _focused = e.Clicked == this;
    }

    public bool OnEnter()
    {
        var error = _onEnter(Text.Replace("_", ""));
        _focused = false;
        _textCursor = 0;
        _text = new char[MaxChars];

        return !error;
    }

    public override void OnKeyEvent(KeyEvent e)
    {
        if (!_focused) return;
        if (e.Type == KeyEventType.Down)
        {
            if (!e.Key.HasValue) return;

            switch (e.Key)
            {
                case Key.Backspace:
                    if (Text.Length > 0)
                    {
                        if (_textCursor - 1 < 0) _textCursor = 1;

                        _text[--_textCursor] = '\0';
                    }

                    break;
                case Key.Return:
                    OnEnter();
                    break;
            }
        }

        if (e.Type == KeyEventType.Text)
        {
            if (!e.Text.HasValue) return;
            if (_textCursor >= MaxChars) return;
            if (e.Text.Value == ' ') return;
            // no numeric input
            if (e.Text.Value >= '0' && e.Text.Value <= '9') return;
            // only allow letters (ignore case)
            if ((e.Text.Value < 'a' || e.Text.Value > 'z') && (e.Text.Value < 'A' || e.Text.Value > 'Z')) return;
            // capitalize
            if (e.Text.Value >= 'a' && e.Text.Value <= 'z') e.Text = (char)(e.Text.Value - 32);
            _text[_textCursor++] = e.Text.Value;

            if (_textCursor > MaxChars) _textCursor = MaxChars;
        }
    }
}