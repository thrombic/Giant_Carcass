using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 3;
    public int contactDamage = 1;
    public bool isInvulnerable = false;

    protected int currentHealth;
    protected bool isDead;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(int amount)
    {
        if (isDead || isInvulnerable || amount <= 0)
            return;

        currentHealth -= amount;
        OnDamaged();

        if (currentHealth <= 0)
            Die();
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    void TryDamagePlayer(GameObject target)
    {
        if (!target.CompareTag("Player"))
            return;

        HealthSystem health = target.GetComponent<HealthSystem>();
        if (health != null)
            health.TakeDamage(contactDamage);
    }
}
