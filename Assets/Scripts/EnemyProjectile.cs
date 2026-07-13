using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile")]
    public float speed = 8f;
    public float lifetime = 10f;
    public int damage = 1;

    [Header("FX")]
    public GameObject hitFXPrefab;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public void Launch(Vector2 direction)
    {
        direction = direction.normalized;
        rb.linearVelocity = direction * speed;
        transform.right = direction;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<EnemyBase>() != null)
            return;

        if (other.GetComponent<EnemyProjectile>() != null)
            return;

        if (other.CompareTag("Player"))
        {
            HealthSystem health = other.GetComponent<HealthSystem>();
            if (health != null)
                health.TakeDamage(damage);
        }

        SpawnHitFX();
        Destroy(gameObject);
    }

    void SpawnHitFX()
    {
        if (hitFXPrefab != null)
            Instantiate(hitFXPrefab, transform.position, Quaternion.identity);
    }
}
