using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Starflea2 : StarfleaBase
{
    [Header("Electric Attack")]
    [FormerlySerializedAs("zapDuration")]
    public float electricDuration = 2f;
    [FormerlySerializedAs("autoZapInterval")]
    public float autoElectricInterval = 5f;
    [FormerlySerializedAs("zapRadius")]
    public float electricRadius = 2f;
    [FormerlySerializedAs("zapDamage")]
    public int electricDamage = 1;
    [FormerlySerializedAs("zapTickRate")]
    public float electricTickRate = 0.25f;
    public LayerMask playerLayer;

    [Header("Electric Animation")]
    public Sprite[] electricAttackSprites;
    public float electricFrameRate = 8f;

    [Header("FX")]
    [FormerlySerializedAs("zapFXPrefab")]
    public GameObject electricFXPrefab;

    private float electricCooldown;
    private bool isElectrified;

    protected override void Start()
    {
        base.Start();
        electricCooldown = autoElectricInterval;
    }

    protected override void Update()
    {
        base.Update();

        if (isDead || isElectrified)
            return;

        electricCooldown -= Time.deltaTime;
        if (electricCooldown <= 0f)
            StartCoroutine(ElectricAttackRoutine());
    }

    protected override void OnDamaged()
    {
        if (!isElectrified)
            StartCoroutine(ElectricAttackRoutine());
    }

    IEnumerator ElectricAttackRoutine()
    {
        isElectrified = true;
        isStopped = true;
        electricCooldown = autoElectricInterval;

        GameObject fx = electricFXPrefab != null
            ? Instantiate(electricFXPrefab, transform.position, Quaternion.identity, transform)
            : null;

        float elapsed = 0f;
        float nextTick = 0f;
        float animationTimer = 0f;
        int animationFrame = 0;
        ShowElectricAttackFrame(animationFrame);

        while (elapsed < electricDuration)
        {
            elapsed += Time.deltaTime;
            nextTick -= Time.deltaTime;
            AnimateElectricAttackSprites(ref animationTimer, ref animationFrame);

            if (nextTick <= 0f)
            {
                DamagePlayersInElectricRadius();
                nextTick = electricTickRate;
            }

            yield return null;
        }

        if (fx != null)
            Destroy(fx);

        ShowCurrentMovementSprite();
        isStopped = false;
        isElectrified = false;
    }

    void AnimateElectricAttackSprites(ref float animationTimer, ref int animationFrame)
    {
        if (electricAttackSprites == null || electricAttackSprites.Length == 0)
            return;

        float frameDuration = 1f / Mathf.Max(0.01f, electricFrameRate);
        animationTimer += Time.deltaTime;

        while (animationTimer >= frameDuration)
        {
            animationTimer -= frameDuration;
            animationFrame = (animationFrame + 1) % electricAttackSprites.Length;
            ShowElectricAttackFrame(animationFrame);
        }
    }

    void ShowElectricAttackFrame(int frame)
    {
        if (electricAttackSprites == null || electricAttackSprites.Length == 0)
            return;

        ApplyVisualSprite(electricAttackSprites[Mathf.Clamp(frame, 0, electricAttackSprites.Length - 1)]);
    }

    void DamagePlayersInElectricRadius()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, electricRadius, playerLayer);
        foreach (Collider2D hit in hits)
        {
            HealthSystem health = hit.GetComponent<HealthSystem>();
            if (health != null)
                health.TakeDamage(electricDamage);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, electricRadius);
    }
}
