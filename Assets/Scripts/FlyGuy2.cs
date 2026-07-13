using System.Collections;
using UnityEngine;

public class FlyGuy2 : FlyGuyBase
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float shootInterval = 5f;
    public float decelerateDuration = 0.5f;

    private float shootTimer;
    private bool isShooting;

    protected override void Start()
    {
        base.Start();
        shootTimer = shootInterval + Random.Range(0f, shootInterval * 0.5f);
    }

    protected override void FixedUpdate()
    {
        if (isDead)
            return;

        if (isShooting)
            return;

        base.FixedUpdate();

        if (state == FlyState.Chase)
        {
            shootTimer -= Time.fixedDeltaTime;
            if (shootTimer <= 0f)
                StartCoroutine(ShootRoutine());
        }
    }

    IEnumerator ShootRoutine()
    {
        isShooting = true;
        shootTimer = shootInterval;

        Vector2 startVelocity = rb.linearVelocity;
        float elapsed = 0f;

        while (elapsed < decelerateDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = decelerateDuration > 0f ? elapsed / decelerateDuration : 1f;
            rb.linearVelocity = Vector2.Lerp(startVelocity, Vector2.zero, t);
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;

        if (player != null && projectilePrefab != null)
        {
            Vector2 aimDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
            EnemyProjectile projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity)
                .GetComponent<EnemyProjectile>();

            if (projectile != null)
                projectile.Launch(aimDirection);
        }

        isShooting = false;
    }
}
