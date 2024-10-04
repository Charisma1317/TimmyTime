internal abstract class UIComponent
{
    protected UIComponent(Vector2 position, Vector2 size, Color color)
    {
        Position = position;
        Size = size;
        Color = color;
    }

    public Screen ParentScreen { get; set; }

    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Color Color { get; set; }

    public virtual Bounds2 Bounds => new Bounds2(Position, Size);

    public bool Hovering { get; set; }
    public bool Clicked { get; set; }

    public abstract void Draw();

    public virtual void OnHover()
    {
    }

    public virtual void OnClick(ClickEvent e)
    {
    }

    public virtual void OnRelease()
    {
    }

    public virtual void OnMouseEnter()
    {
    }

    public virtual void OnMouseLeave()
    {
    }

    public virtual void OnScroll(ScrollType e)
    {
    }

    public virtual void OnKeyEvent(KeyEvent e)
    {
    }
}

enum ScrollType
{
    Up,
    Down
}

struct ClickEvent
{
    public Vector2 MousePosition { get; set; }
    public UIComponent Clicked { get; set; }
}