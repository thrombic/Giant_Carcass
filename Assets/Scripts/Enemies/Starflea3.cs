public class Starflea3 : StarfleaBase
{
    private bool isInvulnerable = false;

    protected override void Awake()
    {
        base.Awake();
        isInvulnerable = true;
    }

    void OnValidate()
    {
        isInvulnerable = true;
    }

    public override void TakeDamage(int amount)
    {
    }

    protected override float CurrentSpeed()
    {
        return UnityEngine.Mathf.Abs(moveSpeed) * 2f;
    }
}
