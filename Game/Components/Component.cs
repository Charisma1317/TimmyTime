internal abstract class Component
{
    public GameObject Parent;
    public bool FirstUpdate { get; set; } = true;
    public Transform Transform => Parent.Transform;

    public virtual void Start()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void FixedUpdate()
    {
    }

    // events
    public virtual void OnCollision(Collision collision)
    {
    }

    public virtual void OnTrigger(Collision collision)
    {
    }
}