using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class StarfleaBase : EnemyBase
{
    [Header("Surface Movement")]
    public float moveSpeed = 1.5f;
    public float bodyRadius = 0.28f;
    public LayerMask surfaceLayer;
    public bool startMovingLeft;
    public float cornerLookAhead = 0.08f;
    public float surfaceSnapDistance = 0.2f;
    public float skinWidth = 0.03f;

    [Header("Airborne")]
    public float airborneGravityScale = 1f;
    public float landingProbeDistance = 0.25f;
    public Sprite fallingSprite;

    [Header("Movement Animation")]
    public Sprite[] movementSprites;
    public float movementFrameRate = 6f;
    public bool flipVisualFacing = true;
    public int visualSortingOrder = 5;

    protected Rigidbody2D rb;
    protected Vector2 moveDir = Vector2.right;
    protected Vector2 surfNormal = Vector2.up;
    protected bool isStopped;

    private bool isAttachedToSurface;
    private SpriteRenderer spriteRenderer;
    private Collider2D ownCollider;
    private Vector3 baseScale;
    private float animationTimer;
    private int animationFrame;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        ownCollider = GetComponent<Collider2D>();
        baseScale = transform.localScale;
        ApplyVisualSorting();
        SetAirbornePhysics();
    }

    protected virtual void Start()
    {
        if (!TryAttachToNearestSurface())
            BeginAirborne();
    }

    protected virtual void Update()
    {
        AnimateMovementSprites();
    }

    void FixedUpdate()
    {
        if (isDead)
            return;

        if (!isAttachedToSurface)
        {
            AirborneStep();
            return;
        }

        if (isStopped)
            return;

        SurfaceStep();
    }

    protected virtual void SurfaceStep()
    {
        Vector2 position = rb.position;
        moveDir = SurfaceTangent(surfNormal) * MovementSign();

        if (!TryTurnIntoWall(ref position) && IsApproachingOuterCorner(position) && !TryWrapOuterCorner(ref position))
        {
            BeginAirborne();
            return;
        }

        if (!SnapToSurface(ref position))
        {
            BeginAirborne();
            return;
        }

        rb.MovePosition(position + moveDir * CurrentSpeed() * Time.fixedDeltaTime);
        ApplyOrientation();
    }

    void AirborneStep()
    {
        ShowFallingSprite();

        float velocityProbe = Mathf.Max(0f, -rb.linearVelocity.y) * Time.fixedDeltaTime;
        float distance = bodyRadius + Mathf.Max(0f, landingProbeDistance) + skinWidth + velocityProbe;
        RaycastHit2D landingSurface = CastSurface(rb.position, Vector2.down, distance);

        if (landingSurface.collider != null)
            AttachToSurface(landingSurface);
    }

    protected virtual float CurrentSpeed()
    {
        return Mathf.Abs(moveSpeed);
    }

    int MovementSign()
    {
        return startMovingLeft ? -1 : 1;
    }

    bool TryTurnIntoWall(ref Vector2 position)
    {
        float distance = bodyRadius + Mathf.Max(0f, cornerLookAhead) + skinWidth;
        RaycastHit2D wall = CastSurface(position + surfNormal * skinWidth, moveDir, distance);

        if (wall.collider == null || Vector2.Dot(wall.normal, surfNormal) > 0.75f)
            return false;

        surfNormal = wall.normal.normalized;
        moveDir = SurfaceTangent(surfNormal) * MovementSign();
        position = wall.point + surfNormal * bodyRadius + moveDir * skinWidth;
        return true;
    }

    bool IsApproachingOuterCorner(Vector2 position)
    {
        Vector2 probeOrigin = position + moveDir * (bodyRadius + Mathf.Max(0f, cornerLookAhead));
        float probeDistance = bodyRadius + Mathf.Max(0f, surfaceSnapDistance) + skinWidth;
        return CastSurface(probeOrigin, -surfNormal, probeDistance).collider == null;
    }

    bool TryWrapOuterCorner(ref Vector2 position)
    {
        int sign = MovementSign();
        Vector2 cornerPoint = position + moveDir * bodyRadius - surfNormal * bodyRadius;
        Vector2 nextNormal = Rotate(surfNormal, -90f * sign).normalized;
        Vector2 nextMoveDir = SurfaceTangent(nextNormal) * sign;

        Vector2 probeOrigin = cornerPoint
            + nextNormal * (bodyRadius + skinWidth)
            + nextMoveDir * (bodyRadius * 0.35f);

        RaycastHit2D nextSurface = CastSurface(probeOrigin, -nextNormal, bodyRadius * 2f + surfaceSnapDistance);

        if (nextSurface.collider != null)
        {
            surfNormal = nextSurface.normal.normalized;
            moveDir = SurfaceTangent(surfNormal) * sign;
            position = nextSurface.point + surfNormal * bodyRadius + moveDir * skinWidth;
            return true;
        }

        return false;
    }

    bool SnapToSurface(ref Vector2 position)
    {
        Vector2 origin = position + surfNormal * skinWidth;
        float distance = bodyRadius + Mathf.Max(0f, surfaceSnapDistance) + skinWidth;
        RaycastHit2D snap = CastSurface(origin, -surfNormal, distance);

        if (snap.collider == null)
            return false;

        surfNormal = snap.normal.normalized;
        moveDir = SurfaceTangent(surfNormal) * MovementSign();
        position = snap.point + surfNormal * bodyRadius;
        return true;
    }

    static Vector2 Rotate(Vector2 value, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos).normalized;
    }

    void ApplyOrientation()
    {
        float angle = Vector2.SignedAngle(Vector2.up, surfNormal);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        float dot = Vector2.Dot(moveDir, SurfaceTangent(surfNormal));
        float facing = dot >= 0f ? 1f : -1f;
        if (flipVisualFacing)
            facing *= -1f;

        transform.localScale = new Vector3(Mathf.Abs(baseScale.x) * facing, baseScale.y, baseScale.z);
    }

    void AnimateMovementSprites()
    {
        if (isDead || isStopped || spriteRenderer == null || movementSprites == null || movementSprites.Length == 0)
            return;

        if (!isAttachedToSurface)
        {
            ShowFallingSprite();
            return;
        }

        float frameDuration = 1f / Mathf.Max(0.01f, movementFrameRate);
        animationTimer += Time.deltaTime;

        while (animationTimer >= frameDuration)
        {
            animationTimer -= frameDuration;
            animationFrame = (animationFrame + 1) % movementSprites.Length;
            ApplyMovementSprite(animationFrame);
        }
    }

    void ApplyMovementSprite(int frame)
    {
        if (spriteRenderer == null || movementSprites == null || movementSprites.Length == 0)
            return;

        ApplyVisualSprite(movementSprites[Mathf.Clamp(frame, 0, movementSprites.Length - 1)]);
    }

    protected void ApplyVisualSprite(Sprite sprite)
    {
        if (spriteRenderer == null || sprite == null)
            return;

        ApplyVisualSorting();
        spriteRenderer.sprite = sprite;
    }

    protected void ShowCurrentMovementSprite()
    {
        ApplyMovementSprite(animationFrame);
    }

    void ShowFallingSprite()
    {
        if (fallingSprite != null)
        {
            ApplyVisualSprite(fallingSprite);
            return;
        }

        ApplyMovementSprite(0);
    }

    void ApplyVisualSorting()
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = visualSortingOrder;
    }

    bool TryAttachToNearestSurface()
    {
        Vector2[] directions = { Vector2.down, Vector2.up, Vector2.left, Vector2.right };
        float distance = bodyRadius
            + Mathf.Max(Mathf.Max(0f, surfaceSnapDistance), Mathf.Max(0f, landingProbeDistance))
            + skinWidth;

        foreach (Vector2 direction in directions)
        {
            RaycastHit2D hit = CastSurface(transform.position, direction, distance);
            if (hit.collider == null)
                continue;

            AttachToSurface(hit);
            return true;
        }

        return false;
    }

    void AttachToSurface(RaycastHit2D hit)
    {
        isAttachedToSurface = true;
        SetSurfaceCrawlPhysics();

        surfNormal = hit.normal.normalized;
        moveDir = SurfaceTangent(surfNormal) * MovementSign();
        rb.position = hit.point + surfNormal * bodyRadius;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        ApplyOrientation();
        ShowCurrentMovementSprite();
    }

    void BeginAirborne()
    {
        isAttachedToSurface = false;
        SetAirbornePhysics();
        rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, rb.linearVelocity.y));
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        ShowFallingSprite();
    }

    void SetSurfaceCrawlPhysics()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    void SetAirbornePhysics()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = Mathf.Max(0f, airborneGravityScale);
    }

    RaycastHit2D CastSurface(Vector2 origin, Vector2 direction, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance, surfaceLayer);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || hit.collider == ownCollider || hit.collider.transform.IsChildOf(transform))
                continue;

            return hit;
        }

        return default;
    }

    static Vector2 SurfaceTangent(Vector2 normal)
    {
        return new Vector2(normal.y, -normal.x).normalized;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Vector2 position = transform.position;
        Vector2 tangent = SurfaceTangent(surfNormal) * MovementSign();

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, bodyRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, (Vector3)surfNormal * 0.6f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, (Vector3)tangent * 0.6f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(position + surfNormal * skinWidth, tangent * (bodyRadius + cornerLookAhead + skinWidth));

        Gizmos.color = Color.magenta;
        Vector2 outerProbe = position + tangent * (bodyRadius + cornerLookAhead);
        Gizmos.DrawRay(outerProbe, -surfNormal * (bodyRadius + surfaceSnapDistance + skinWidth));

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(position + surfNormal * skinWidth, -surfNormal * (bodyRadius + surfaceSnapDistance + skinWidth));
    }
}
