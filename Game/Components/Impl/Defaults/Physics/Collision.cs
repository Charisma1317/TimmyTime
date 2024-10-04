internal struct Collision
{
    public BoxCollider OtherCollider;
    public GameObject Other;
    public float Time;
    public Vector2 Position;
    public Vector2 Normal;
    public bool IsHit;

    public override string ToString()
    {
        return $"Collision: {Other.OriginalPrefab} at {Position} with normal {Normal}";
    }
}