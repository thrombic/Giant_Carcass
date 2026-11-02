using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 20;
    public float invulnerabilitySeconds = 1f;

    public int CurrentHealth { get; private set; }
    public event System.Action Died;

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
        print("Took " + amount + " damage," + "Current health: " + CurrentHealth);
        if (amount <= 0 || invulnerabilityTimer > 0f || CurrentHealth <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        invulnerabilityTimer = invulnerabilitySeconds;

        if (CurrentHealth == 0)
            Die();

        return;
    }

    public void GrantInvulnerability(float seconds)
    {
        invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, Mathf.Max(0f, seconds));
    }

    public bool Heal(int amount)
    {
        if (amount <= 0)
            return false;

        if (CurrentHealth <= 0)
        {
            PlayerEnergy playerEnergy = GetComponent<PlayerEnergy>();
            if (playerEnergy == null || !playerEnergy.AllowPlayAtZeroEnergy)
                return false;
        }

        int healthBeforeHealing = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        return CurrentHealth > healthBeforeHealing;
    }

    public void SetMaxHealth(int amount, bool refill)
    {
        maxHealth = Mathf.Max(1, amount);
        CurrentHealth = refill ? maxHealth : Mathf.Min(CurrentHealth, maxHealth);
    }

    void Die()
    {
        Died?.Invoke();
        Debug.Log($"{name} died.");
    }
}
