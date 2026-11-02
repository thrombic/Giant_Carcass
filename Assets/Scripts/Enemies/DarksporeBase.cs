using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(LightDetector))]
public class DarksporeBase : EnemyBase
{
    [Header("Dormant Drift")]
    public float dormantSpeed = 0.6f;
    public float idleRadius = 1.25f;
    public float idleFrequency = 0.45f;

    [Header("Awakened Flight")]
    public float chaseSpeed = 1.6f;
    public float detectionRadius = 6f;
    [Range(0f, 1f)] public float meanderStrength = 0.45f;
    public float meanderFrequency = 1.35f;
    public float steering = 4f;

    protected Rigidbody2D rb;
    protected Transform player;
    protected bool isAlerted;

    private LightDetector lightDetector;
    private Vector2 idleCenter;
    private float movementPhase;
    private float lightWakeTimer;
    private float playerSearchTimer;

    protected bool IsAlerted => isAlerted;
    protected virtual bool CanAct => true;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        lightDetector = GetComponent<LightDetector>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearDamping = 2.5f;
        movementPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    protected virtual void Start()
    {
        idleCenter = transform.position;
        FindPlayer();
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || !CanAct)
            return;

        if (player == null)
        {
            playerSearchTimer -= Time.fixedDeltaTime;
            if (playerSearchTimer <= 0f)
                FindPlayer();

            DriftWhileDormant();
            return;
        }

        if (!isAlerted && ShouldWake())
        {
            isAlerted = true;
            OnAlerted();
        }

        if (isAlerted)
            ChasePlayer();
        else
            DriftWhileDormant();
    }

    protected virtual void OnAlerted()
    {
    }

    protected void ReturnToDormant()
    {
        isAlerted = false;
        lightWakeTimer = 0f;
        idleCenter = transform.position;
    }

    private bool ShouldWake()
    {
        if (Vector2.Distance(transform.position, player.position) <= detectionRadius)
            return true;

        if (lightDetector == null || lightDetector.CurrentExposure < lightDetector.exposureThreshold)
        {
            lightWakeTimer = 0f;
            return false;
        }

        lightWakeTimer += Time.fixedDeltaTime;
        return lightWakeTimer >= Mathf.Max(0f, lightDetector.detectionDelay);
    }

    private void DriftWhileDormant()
    {
        float time = Time.time * idleFrequency + movementPhase;
        Vector2 target = idleCenter + new Vector2(
            Mathf.Cos(time) * idleRadius,
            Mathf.Sin(time * 1.3f) * idleRadius * 0.55f);
        SteerTowards(target, dormantSpeed);
    }

    private void ChasePlayer()
    {
        Vector2 toPlayer = ((Vector2)player.position - rb.position).normalized;
        float angle = Time.time * meanderFrequency * Mathf.PI * 2f + movementPhase;
        Vector2 circularOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * meanderStrength;
        Vector2 direction = (toPlayer + circularOffset).normalized;
        Vector2 desiredVelocity = direction * chaseSpeed;
        rb.linearVelocity = Vector2.Lerp(
            rb.linearVelocity,
            desiredVelocity,
            Mathf.Clamp01(steering * Time.fixedDeltaTime));
    }

    private void SteerTowards(Vector2 target, float speed)
    {
        Vector2 direction = (target - rb.position).normalized;
        Vector2 desiredVelocity = direction * speed;
        rb.linearVelocity = Vector2.Lerp(
            rb.linearVelocity,
            desiredVelocity,
            Mathf.Clamp01(steering * Time.fixedDeltaTime));
    }

    private void FindPlayer()
    {
        PlayerController controller = FindFirstObjectByType<PlayerController>();
        player = controller != null ? controller.transform : null;
        playerSearchTimer = 1f;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
