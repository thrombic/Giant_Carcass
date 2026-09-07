using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 20;
    public float invulnerabilitySeconds = 0.5f;

    public int CurrentHealth { get; private set; }

    public bool IsInvulnerable => invulnerabilityTimer > 0f;

    private float invulnerabilityTimer;

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    void Update()
    {
        if (invulnerabilityTimer > 0f)
            invulnerabilityTimer -= Time.deltaTime;
    }

    public void TakeDamage(int amount)
    {
        TryTakeDamage(amount);
    }

    public bool TryTakeDamage(int amount)
    {
        if (amount <= 0 || invulnerabilityTimer > 0f || CurrentHealth <= 0)
            return false;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        invulnerabilityTimer = invulnerabilitySeconds;

        if (CurrentHealth == 0)
            Die();

        return true;
    }

    public void GrantInvulnerability(float seconds)
    {
        invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, Mathf.Max(0f, seconds));
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0)
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
    }

    void Die()
    {
        Debug.Log($"{name} died.");
    }
}
