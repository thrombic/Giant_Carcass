using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 5;
    public float invulnerabilitySeconds = 0.5f;

    [SerializeField] private int currentHealth;

    public int CurrentHealth => currentHealth;
    public bool IsInvulnerable => invulnerabilityTimer > 0f;

    private float invulnerabilityTimer;

    void Awake()
    {
        currentHealth = maxHealth;
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
        if (amount <= 0 || invulnerabilityTimer > 0f || currentHealth <= 0)
            return false;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        invulnerabilityTimer = invulnerabilitySeconds;

        if (currentHealth == 0)
            Die();

        return true;
    }

    public void GrantInvulnerability(float seconds)
    {
        invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, Mathf.Max(0f, seconds));
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || currentHealth <= 0)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    void Die()
    {
        Debug.Log($"{name} died.");
    }
}
