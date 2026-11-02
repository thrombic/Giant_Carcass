using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile")]
    public float speed = 8f;
    public float lifetime = 10f;
    public float maxTravelDistance = 28f;
    public int damage = 1;

    [Header("Homing")]
    public float homingTurnRate = 75f;

    [Header("FX")]
    public GameObject hitFXPrefab;

    private Rigidbody2D rb;
    private Transform homingTarget;
    private bool homing;
    private Vector2 lastPosition;
    private float distanceTraveled;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        lastPosition = transform.position;
    }

    public void Launch(Vector2 direction, Transform target = null, bool shouldHome = false)
    {
        direction = direction.normalized;
        homingTarget = target;
        homing = shouldHome;
        lastPosition = transform.position;
        rb.linearVelocity = direction * speed;
        transform.right = direction;
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        if (homing && homingTarget != null && rb.linearVelocity.sqrMagnitude > 0.001f)
        {
            Vector2 currentDirection = rb.linearVelocity.normalized;
            Vector2 desiredDirection = ((Vector2)homingTarget.position - rb.position).normalized;
            float maxRadians = homingTurnRate * Mathf.Deg2Rad * Time.fixedDeltaTime;
            Vector2 steeredDirection = Vector3.RotateTowards(
                currentDirection,
                desiredDirection,
                maxRadians,
                0f);

            rb.linearVelocity = steeredDirection.normalized * speed;
            transform.right = steeredDirection;
        }

        Vector2 currentPosition = transform.position;
        distanceTraveled += Vector2.Distance(lastPosition, currentPosition);
        lastPosition = currentPosition;

        if (maxTravelDistance > 0f && distanceTraveled >= maxTravelDistance)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<EnemyBase>() != null)
            return;

        if (other.GetComponentInParent<EnemyProjectile>() != null)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            HealthSystem health = player.GetComponent<HealthSystem>();
            if (health != null)
                health.TakeDamage(damage);
        }
        else if (other.isTrigger || other.GetComponentInParent<Bullet>() != null ||
                 other.GetComponentInParent<Flare>() != null)
        {
            return;
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
