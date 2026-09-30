using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public int contactDamage = 1;
    public HealthSystem healthSystem;

    protected int currentHealth;
    protected bool isDead;
    protected bool contactDamageEnabled = true;
    protected SpriteRenderer spriteRenderer;
    protected Color originalColor;

    protected virtual void Awake()
    {
        healthSystem = gameObject.AddComponent<HealthSystem>();
        currentHealth = healthSystem.maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(.25f);
        spriteRenderer.color = originalColor;
    }

    public virtual void TakeDamage(int amount)
    {
        if (healthSystem.IsInvulnerable)
            return;

        StartCoroutine(FlashRoutine());
        healthSystem.TakeDamage(amount);
    }

    protected virtual void OnDamaged()
    {
    }

    protected virtual void Die()
    {
        isDead = true;
        OnDeath();
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
        PlayerDamageReceiver playerDamageReceiver = playerHealth.GetComponent<PlayerDamageReceiver>();

        if (playerHealth != null && !playerDamageReceiver.IsInvincible)
        {
            playerHealth.TakeDamage(contactDamage);
            StartCoroutine(playerDamageReceiver.ApplyStunAndKnockback((target.transform.position - transform.position).normalized, knockbackForce));
        }
    }
}
