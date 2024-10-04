internal abstract class Goal<T>
{
    protected GameObject _parent;

    protected Goal(GameObject parent)
    {
        _parent = parent;
    }

    public bool Enabled { get; set; } = false;

    public abstract T Execute();
}