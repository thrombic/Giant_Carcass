using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 5;
    public float invulnerabilitySeconds = 0.5f;

    public int CurrentHealth { get; private set; }

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
        if (amount <= 0 || invulnerabilityTimer > 0f || CurrentHealth <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        invulnerabilityTimer = invulnerabilitySeconds;

        if (CurrentHealth == 0)
            Die();
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
