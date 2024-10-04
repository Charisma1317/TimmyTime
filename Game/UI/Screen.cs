using System;
using System.Collections.Generic;
using System.Diagnostics;

internal class Screen
{
    private int keyListenerId = -1;
    public HashSet<UIComponent> Components { get; set; } = new HashSet<UIComponent>();

    private readonly Queue<UIComponent> _toRemove = new Queue<UIComponent>();
    private readonly Queue<UIComponent> _toAdd = new Queue<UIComponent>();

    public bool DisabledMouseInput = false;
    public bool DisabledKeyboardInput = false;

    public void Draw()
    {
        var components = new List<UIComponent>(Components);
        foreach (var component in components)
        {
            component.ParentScreen = this;
            component.Draw();
        }
    }

    public T GetComponent<T>() where T : UIComponent
    {
        var components = new List<UIComponent>(Components);

        foreach (var component in components)
        {
            if (component is T t) return t;
        }

        return null;
    }

    public void Add(UIComponent component)
    {
        if (Game.CurrentScreenState == Game.ScreenState.Game)
            _toAdd.Enqueue(component);
        else
            Components.Add(component);
    }

    public void Remove(UIComponent component)
    {
        _toRemove.Enqueue(component);
    }

    public bool Contains(UIComponent component)
    {
        if (_toAdd.Contains(component)) return true;
        return !_toRemove.Contains(component) && Components.Contains(component);
    }

    public void ResetComponentState()
    {
        if (keyListenerId != -1)
        {
            Engine.RemoveKeyListener(keyListenerId);
            keyListenerId = -1;
        }

        DisabledMouseInput = false;
        DisabledKeyboardInput = false;

        foreach (var component in Components)
        {
            component.ParentScreen = this;
            OnRelease(component);
            OnMouseLeave(component);

            component.Hovering = false;
            component.Clicked = false;
        }
    }

    public void Update()
    {
        while (_toRemove.Count > 0)
        {
            var comp = _toRemove.Dequeue();
            Components.Remove(comp);
        }

        while (_toAdd.Count > 0)
        {
            var comp = _toAdd.Dequeue();
            Components.Add(comp);
        }

        if (keyListenerId == -1)
            keyListenerId = Engine.AddKeyListener(e =>
            {
                foreach (var component in Components) OnKeyEvent(component, e);
            });

        // check for mouse hover
        foreach (var component in Components)
        {
            component.ParentScreen = this;

            if (Engine.MouseScroll != 0)
            {
                component.OnScroll(Engine.MouseScroll > 0 ? ScrollType.Up : ScrollType.Down);
            }

            if (component.Bounds.Contains(Engine.MousePosition) && !component.Hovering)
            {
                OnMouseEnter(component);
                OnHover(component);
                component.Hovering = true;
            }

            if (!component.Bounds.Contains(Engine.MousePosition) && component.Hovering)
            {
                OnMouseLeave(component);
                component.Hovering = false;
            }

            if (component.Hovering && Engine.GetMouseButtonDown(MouseButton.Left) && !component.Clicked)
            {
                BroadcastOnClick(new ClickEvent
                    { MousePosition = Engine.MousePosition, Clicked = component });
                component.Clicked = true;
            }

            if (component.Clicked && Engine.GetMouseButtonUp(MouseButton.Left))
            {
                OnRelease(component);
                component.Clicked = false;
            }
        }
    }

    public void BroadcastOnClick(ClickEvent e)
    {
        foreach (var component in Components) component.OnClick(e);
    }

    public void OnHover(UIComponent comp)
    {
        comp.OnHover();
    }

    public void OnRelease(UIComponent comp)
    {
        comp.OnRelease();
    }

    public void OnMouseEnter(UIComponent comp)
    {
        comp.OnMouseEnter();
    }

    public void OnMouseLeave(UIComponent comp)
    {
        comp.OnMouseLeave();
    }

    public void OnKeyEvent(UIComponent comp, KeyEvent e)
    {
        comp.OnKeyEvent(e);
    }

    public void Clear()
    {
        Components.Clear();
    }
}