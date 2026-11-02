using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    [Min(1)] public int maxHealth = 3;
    public int contactDamage = 1;
    public HealthSystem healthSystem;

    [Header("Drops")]
    public DropTable dropTable;

    protected int currentHealth;
    protected bool isDead;
    protected bool contactDamageEnabled = true;
    protected SpriteRenderer spriteRenderer;
    protected Color originalColor;

    protected virtual void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
            healthSystem = gameObject.AddComponent<HealthSystem>();

        healthSystem.SetMaxHealth(maxHealth, true);
        currentHealth = healthSystem.maxHealth;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null)
            yield break;

        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(.25f);
        spriteRenderer.color = originalColor;
    }

    public virtual void TakeDamage(int amount)
    {
        if (isDead || healthSystem == null || healthSystem.IsInvulnerable)
            return;

        int healthBeforeDamage = healthSystem.CurrentHealth;
        StartCoroutine(FlashRoutine());
        healthSystem.TakeDamage(amount);
        currentHealth = healthSystem.CurrentHealth;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (currentHealth < healthBeforeDamage)
            OnDamaged();
    }

    protected virtual void OnDamaged()
    {
    }

    protected virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;
        OnDeath();
        if (dropTable != null)
            dropTable.TrySpawnDrop(transform.position);
        Destroy(gameObject, 0.15f);
    }

    protected virtual void OnDeath()
    {
    }

    protected void OnTriggerStay2D(Collider2D other)
    {
        if (contactDamageEnabled)
            TryDamagePlayer(other.gameObject, 5f);
    }

    protected void TryDamagePlayer(GameObject target, float knockbackForce)
    {
        if (target.layer != LayerMask.NameToLayer("Player"))
            return;

        HealthSystem playerHealth = target.GetComponent<HealthSystem>();
        PlayerDamageReceiver playerDamageReceiver = target.GetComponent<PlayerDamageReceiver>();

        if (playerHealth != null && playerDamageReceiver != null && !playerDamageReceiver.IsInvincible)
        {
            playerHealth.TakeDamage(contactDamage);
            StartCoroutine(playerDamageReceiver.ApplyStunAndKnockback((target.transform.position - transform.position).normalized, knockbackForce));
        }
    }
}
