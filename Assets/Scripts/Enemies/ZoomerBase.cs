using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ZoomerBase : EnemyBase
{
    [Header("Surface Movement")]
    public float moveSpeed = 1.5f;
    public float bodyRadius = 0.28f;
    public LayerMask surfaceLayer;
    public bool startMovingLeft;
    public float cornerLookAhead = 0.08f;
    public float surfaceSnapDistance = 0.2f;
    public float skinWidth = 0.03f;

    [Header("Movement Animation")]
    public Sprite[] movementSprites;
    public float movementFrameRate = 6f;
    public bool flipVisualFacing = true;
    public int visualSortingOrder = 5;

    protected Rigidbody2D rb;
    protected Vector2 moveDir = Vector2.right;
    protected Vector2 surfNormal = Vector2.up;
    protected bool isStopped;

    private SpriteRenderer spriteRenderer;
    private Collider2D ownCollider;
    private Vector3 baseScale;
    private float animationTimer;
    private int animationFrame;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        ownCollider = GetComponent<Collider2D>();
        baseScale = transform.localScale;
        ApplyVisualSorting();
    }

    protected virtual void Start()
    {
        AttachToNearestSurface();
        ApplyMovementSprite(0);
    }

    protected virtual void Update()
    {
        AnimateMovementSprites();
    }

    void FixedUpdate()
    {
        if (isDead || isStopped)
            return;

        SurfaceStep();
    }

    protected virtual void SurfaceStep()
    {
        Vector2 position = rb.position;
        moveDir = SurfaceTangent(surfNormal) * MovementSign();

        if (!TryTurnIntoWall(ref position) && IsApproachingOuterCorner(position))
            WrapOuterCorner(ref position);

        SnapToSurface(ref position);
        rb.MovePosition(position + moveDir * CurrentSpeed() * Time.fixedDeltaTime);
        ApplyOrientation();
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

    void WrapOuterCorner(ref Vector2 position)
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
            return;
        }

        surfNormal = nextNormal;
        moveDir = nextMoveDir;
        position = cornerPoint + surfNormal * bodyRadius + moveDir * skinWidth;
    }

    void SnapToSurface(ref Vector2 position)
    {
        Vector2 origin = position + surfNormal * skinWidth;
        float distance = bodyRadius + Mathf.Max(0f, surfaceSnapDistance) + skinWidth;
        RaycastHit2D snap = CastSurface(origin, -surfNormal, distance);

        if (snap.collider == null)
            return;

        surfNormal = snap.normal.normalized;
        moveDir = SurfaceTangent(surfNormal) * MovementSign();
        position = snap.point + surfNormal * bodyRadius;
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

        ApplyVisualSorting();
        spriteRenderer.sprite = movementSprites[Mathf.Clamp(frame, 0, movementSprites.Length - 1)];
    }

    void ApplyVisualSorting()
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = visualSortingOrder;
    }

    void AttachToNearestSurface()
    {
        Vector2[] directions = { Vector2.down, Vector2.up, Vector2.left, Vector2.right };

        foreach (Vector2 direction in directions)
        {
            RaycastHit2D hit = CastSurface(transform.position, direction, 1.5f);
            if (hit.collider == null)
                continue;

            surfNormal = hit.normal;
            moveDir = SurfaceTangent(surfNormal) * MovementSign();
            transform.position = hit.point + surfNormal * bodyRadius;
            ApplyOrientation();
            return;
        }
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
