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
        if (!healthSystem.IsInvulnerable)
        {
            StartCoroutine(FlashRoutine());
        }

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

    void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    void TryDamagePlayer(GameObject target)
    {
        if (!target.CompareTag("Player"))
            return;

        HealthSystem playerHealth = target.GetComponent<HealthSystem>();
        if (playerHealth != null)
            playerHealth.TakeDamage(contactDamage);
    }
}
