internal class Physics
{
    public static readonly float GravityConstant = 9.8f;
    public static readonly float FrictionConstant = 0.8f;
    public static readonly float IdealFixedDeltaTime = 1 / 60.0f;

    public static float FixedDeltaTime = IdealFixedDeltaTime;

    public static float PlayerSpeed = 3000f;
    public static float PlayerJumpHeight = 275f;
    public static readonly float PlayerMass = 60f;
    public static readonly float PlayerWallFallOffRate = 1f;

    public static readonly float LadderSpeed = 200f;

    public static void changeSpeed(float multiplier)
    {
        PlayerSpeed *= multiplier;
    }
    public static void changeJump(float multiplier)
    {
        PlayerJumpHeight *= multiplier;
    }
    public static void setSpeed()
    {
        PlayerSpeed = 3000f;
    }
    public static void setJump()
    {
        PlayerJumpHeight = 275f;
    }
}