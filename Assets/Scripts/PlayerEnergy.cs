using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(HealthSystem))]
public sealed class PlayerEnergy : MonoBehaviour
{
    public const int EnergyPerTank = 100;
    public const int BaseEnergy = 99;
    public const int MaximumTanks = 14;

    [SerializeField, Range(0, MaximumTanks)] private int energyTanks = 2;

    [Header("Temporary Testing")]
    [Tooltip("Testing only: keep moving at zero energy and allow health pickups to revive the player.")]
    [SerializeField] private bool allowPlayAtZeroEnergy = true;

    [Header("Game Over")]
    public UnityEvent onGameOver = new UnityEvent();

    public int EnergyTanks => energyTanks;
    public int Capacity => BaseEnergy + EnergyPerTank * energyTanks;
    public int DisplayEnergy => health != null ? health.CurrentHealth % EnergyPerTank : BaseEnergy;
    public int FilledTanks => health != null ? health.CurrentHealth / EnergyPerTank : energyTanks;
    public bool AllowPlayAtZeroEnergy => allowPlayAtZeroEnergy;
    public bool IsGameOver { get; private set; }

    private HealthSystem health;

    private void Awake()
    {
        health = GetComponent<HealthSystem>();
        health.SetMaxHealth(Capacity, true);
        health.Died += HandleDeath;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.Died -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (allowPlayAtZeroEnergy)
            return;

        IsGameOver = true;
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.SetControlsEnabled(false);
        onGameOver.Invoke();
    }

    public bool AddEnergyTank(bool refill = true)
    {
        if (energyTanks >= MaximumTanks)
            return false;

        energyTanks++;
        health.SetMaxHealth(Capacity, refill);
        return true;
    }
}
