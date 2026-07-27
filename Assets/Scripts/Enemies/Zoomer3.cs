public class Zoomer3 : ZoomerBase
{
    protected override void Awake()
    {
        base.Awake();
        isInvulnerable = true;
    }

    protected override float CurrentSpeed()
    {
        return UnityEngine.Mathf.Abs(moveSpeed) * 2f;
    }
}
