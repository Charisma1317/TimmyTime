using System;

internal class Button : Text
{
    private readonly Action<ClickEvent> _callback;

    private Color _originalColor;
    public Button(Action<ClickEvent> callback, TextProperties properties, Vector2 pos, Color buttonColor,
        TextBox linkedTextBox = null) :
        base(properties,
            pos, buttonColor)
    {
        Properties = properties;
        _callback = callback;
        LinkedTextBox = linkedTextBox;
        _originalColor = buttonColor;
    }

    public TextBox LinkedTextBox { get; set; }

    public override void Draw()
    {
        // recalculates the size of the text
        if (!Properties.HasValue) return;

        base.Draw();

    }

    public static Button GameButton(Action<ClickEvent> callback, string text, Color textColor, Color bgColor,
        Vector2 pos)
    {
        var props = new TextProperties(text, Game.MediumFont, textColor, TextAlignment.Center);
        // var textBounds = new Bounds2(pos, new Vector2(320, 50));

        return new Button(callback, props, pos, bgColor);
    }

    public static Button SmallGameButton(Action<ClickEvent> callback, string text, Color textColor, Color bgColor,
        Vector2 pos)
    {
        var props = new TextProperties(text, Game.SmallFont, textColor, TextAlignment.Center);
        // var textBounds = new Bounds2(pos, new Vector2(320, 50));

        return new Button(callback, props, pos, bgColor);
    }

    public static Button GameButton(Action<ClickEvent> callback, string text, Color textColor, Color bgColor,
        Vector2 pos, TextBox linkedTextBox)
    {
        var props = new TextProperties(text, Game.MediumFont, textColor, TextAlignment.Center);
        // var textBounds = new Bounds2(pos, new Vector2(320, 50));

        return new Button(callback, props, pos, bgColor, linkedTextBox);
    }

    public static Button MenuButton(Game.ScreenState state, Screen screen, string text, Color textColor, Color bgColor,
        Vector2 pos, bool set = false)
    {
        var props = new TextProperties(text, Game.MediumFont, textColor, TextAlignment.Center);
        // var textBounds = new Bounds2(pos, new Vector2(320, 50));

        return new Button(Callback, props, pos, bgColor);

        void Callback(ClickEvent e)
        {
            Game.CurrentScreenState = state;
            if (set)
                ScreenManager.SetScreen(screen);
            else
                ScreenManager.SwitchScreen(screen);
        }
    }

    public static Button SmallMenuButton(Game.ScreenState state, Screen screen, string text, Color textColor, Color bgColor,
        Vector2 pos, bool set = false)
    {
        var props = new TextProperties(text, Game.SmallFont, textColor, TextAlignment.Center);
        // var textBounds = new Bounds2(pos, new Vector2(320, 50));

        return new Button(Callback, props, pos, bgColor);

        void Callback(ClickEvent e)
        {
            Game.CurrentScreenState = state;
            if (set)
                ScreenManager.SetScreen(screen);
            else
                ScreenManager.SwitchScreen(screen);
        }
    }

    public static Button BackButton(string text, Color textColor, Color bgColor,
        Vector2 pos)
    {
        var props = new TextProperties(text, Game.MediumFont, textColor, TextAlignment.Center);
        // var textBounds = new Bounds2(pos, new Vector2(320, 50));

        return new Button(Callback, props, pos, bgColor);

        void Callback(ClickEvent e)
        {
            ScreenManager.GoBack();
        }
    }

    // its inherited from Text, so jic, we dont want to use it
    [Obsolete("Use Text.TitleText instead", true)]
    public static Text TitleText(string text, Color textColor, Color bgColor, Vector2 pos)
    {
        throw new Exception("Use Text.TileText instead");
    }

    public override void OnClick(ClickEvent e)
    {
        if (e.Clicked != this) return;

        var b = LinkedTextBox?.OnEnter();
        if (b.HasValue && !b.Value)
            return;

        _callback.Invoke(e);
        Color = _originalColor.Darken(0.25f);
    }

    public override void OnRelease()
    {
        base.OnRelease();
        Color = _originalColor.Darken(0.1f);
    }

    public override void OnMouseEnter()
    {
        base.OnMouseEnter();
        // change color to be darker
        Color = _originalColor.Darken(0.1f);

        ParentScreen.DisabledMouseInput = true;
    }

    public override void OnMouseLeave()
    {
        base.OnMouseLeave();
        // change color to be lighter
        Color = _originalColor;

        ParentScreen.DisabledMouseInput = false;
    }
}