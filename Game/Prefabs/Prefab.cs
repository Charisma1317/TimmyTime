using System;

internal class Prefab : Attribute
{
    public Prefab(Type componentType) : this(componentType, false)
    {
    }

    public Prefab(Type componentType, bool overrideFree)
    {
        ComponentType = componentType;
        OverrideFree = overrideFree;
    }

    public Type ComponentType { get; set; }

    public bool OverrideFree { get; set; } = false;
}