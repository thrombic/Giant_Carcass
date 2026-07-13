using System.Collections;
using UnityEngine;

public class Zoomer2 : ZoomerBase
{
    [Header("Zap Attack")]
    public float zapDuration = 2f;
    public float autoZapInterval = 5f;
    public float zapRadius = 2f;
    public int zapDamage = 1;
    public float zapTickRate = 0.25f;
    public LayerMask playerLayer;

    [Header("FX")]
    public GameObject zapFXPrefab;

    private float zapCooldown;
    private bool isZapping;

    protected override void Start()
    {
        base.Start();
        zapCooldown = autoZapInterval;
    }

    protected override void Update()
    {
        base.Update();

        if (isDead || isZapping)
            return;

        zapCooldown -= Time.deltaTime;
        if (zapCooldown <= 0f)
            StartCoroutine(ZapRoutine());
    }

    protected override void OnDamaged()
    {
        if (!isZapping)
            StartCoroutine(ZapRoutine());
    }

    IEnumerator ZapRoutine()
    {
        isZapping = true;
        isStopped = true;
        zapCooldown = autoZapInterval;

        GameObject fx = zapFXPrefab != null
            ? Instantiate(zapFXPrefab, transform.position, Quaternion.identity, transform)
            : null;

        float elapsed = 0f;
        float nextTick = 0f;

        while (elapsed < zapDuration)
        {
            elapsed += Time.deltaTime;
            nextTick -= Time.deltaTime;

            if (nextTick <= 0f)
            {
                DamagePlayersInZapRadius();
                nextTick = zapTickRate;
            }

            yield return null;
        }

        if (fx != null)
            Destroy(fx);

        isStopped = false;
        isZapping = false;
    }

    void DamagePlayersInZapRadius()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, zapRadius, playerLayer);
        foreach (Collider2D hit in hits)
        {
            HealthSystem health = hit.GetComponent<HealthSystem>();
            if (health != null)
                health.TakeDamage(zapDamage);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, zapRadius);
    }
}
