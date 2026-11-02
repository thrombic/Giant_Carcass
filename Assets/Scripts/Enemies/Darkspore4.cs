using UnityEngine;

public class Darkspore4 : DarksporeBase, IFlareReactive
{
    [Header("Flare Stun")]
    public float flareStunDuration = 4f;
    public Color stunnedTint = new Color(0.35f, 0.45f, 0.65f, 1f);

    private bool isFlareStunned;
    private float stunTimer;
    private int lastFlareInstanceId;

    protected override bool CanAct => !isFlareStunned;

    protected override void FixedUpdate()
    {
        if (!isFlareStunned)
        {
            base.FixedUpdate();
            return;
        }

        stunTimer -= Time.fixedDeltaTime;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 10f * Time.fixedDeltaTime);

        if (stunTimer > 0f)
            return;

        isFlareStunned = false;
        contactDamageEnabled = true;
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        ReturnToDormant();
    }

    public void OnFlareContact(Flare source)
    {
        if (source == null || source.GetInstanceID() == lastFlareInstanceId)
            return;

        lastFlareInstanceId = source.GetInstanceID();
        isFlareStunned = true;
        stunTimer = Mathf.Max(0.1f, flareStunDuration);
        contactDamageEnabled = false;
        rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null)
            spriteRenderer.color = stunnedTint;
    }

    public override void TakeDamage(int amount)
    {
        // Darkspore4 is intentionally immune to ordinary damage for now.
    }
}
