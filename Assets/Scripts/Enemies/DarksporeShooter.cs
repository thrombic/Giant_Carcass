using UnityEngine;

public class DarksporeShooter : DarksporeBase
{
    [Header("Projectile Attack")]
    public GameObject projectilePrefab;
    public float shootInterval = 2.75f;
    public float firstShotDelay = 1.25f;
    public float projectileSpawnOffset = 0.7f;
    public bool homingProjectile;

    private float shootTimer;

    protected override void Start()
    {
        base.Start();
        shootTimer = firstShotDelay;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (isDead || !CanAct || !IsAlerted || player == null || projectilePrefab == null)
            return;

        shootTimer -= Time.fixedDeltaTime;
        if (shootTimer > 0f)
            return;

        Shoot();
        shootTimer = Mathf.Max(0.1f, shootInterval);
    }

    private void Shoot()
    {
        Vector2 direction = ((Vector2)player.position - rb.position).normalized;
        Vector3 spawnPosition = transform.position + (Vector3)(direction * projectileSpawnOffset);
        EnemyProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity)
            .GetComponent<EnemyProjectile>();

        if (projectile != null)
            projectile.Launch(direction, player, homingProjectile);
    }
}
