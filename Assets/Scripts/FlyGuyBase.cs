using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyGuyBase : EnemyBase
{
    public enum FlyState
    {
        Wander,
        Chase
    }

    [Header("Wander")]
    public float wanderSpeed = 2f;
    public float wanderRadius = 4f;
    public float dirChangeInterval = 2f;

    [Header("Chase")]
    public float chaseSpeed = 5f;
    public float detectionRadius = 5f;
    public float loseRadius = 8f;

    [Header("Smoothing")]
    public float steerLerp = 4f;

    protected Rigidbody2D rb;
    protected FlyState state = FlyState.Wander;
    protected Transform player;

    private Vector2 spawnPos;
    private Vector2 wanderTarget;
    private float dirTimer;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 3f;
    }

    protected virtual void Start()
    {
        spawnPos = transform.position;
        wanderTarget = RandomWanderPoint();
        dirTimer = dirChangeInterval;
        player = GameObject.FindWithTag("Player")?.transform;
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || player == null)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (state == FlyState.Wander && distanceToPlayer <= detectionRadius)
            state = FlyState.Chase;
        else if (state == FlyState.Chase && distanceToPlayer > loseRadius)
            state = FlyState.Wander;

        switch (state)
        {
            case FlyState.Wander:
                Wander();
                break;
            case FlyState.Chase:
                Chase();
                break;
        }

        FaceVelocity();
    }

    void Wander()
    {
        dirTimer -= Time.fixedDeltaTime;

        if (dirTimer <= 0f || Vector2.Distance(transform.position, wanderTarget) < 0.4f)
        {
            wanderTarget = RandomWanderPoint();
            dirTimer = dirChangeInterval;
        }

        Steer(wanderTarget, wanderSpeed);
    }

    protected virtual void Chase()
    {
        Steer(player.position, chaseSpeed);
    }

    protected void Steer(Vector2 target, float speed)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, direction * speed, Time.fixedDeltaTime * steerLerp);
    }

    Vector2 RandomWanderPoint()
    {
        return spawnPos + Random.insideUnitCircle * wanderRadius;
    }

    void FaceVelocity()
    {
        if (rb.linearVelocity.sqrMagnitude < 0.05f)
            return;

        float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0f, 0f, angle),
            Time.fixedDeltaTime * 10f);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying ? spawnPos : (Vector2)transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, loseRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(origin, wanderRadius);
    }
}
