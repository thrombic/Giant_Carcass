using System.Collections;
using UnityEngine;

[RequireComponent(typeof(HealthSystem), typeof(Rigidbody2D))]
public class PlayerDamageReceiver : MonoBehaviour
{
    [Header("Hazard Reset Animation")]
    public Animator animator;
    public string hazardResetTrigger = "HazardReset";
    public Sprite hazardResetSprite;
    public Color hazardResetTint = new Color(1f, 0.45f, 0.15f, 1f);

    public bool IsResetting { get; private set; }

    private HealthSystem health;
    private Rigidbody2D rb;
    private PlayerController controller;
    private SpriteRenderer spriteRenderer;
    private Vector3 startingPosition;

    void Awake()
    {
        health = GetComponent<HealthSystem>();
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        startingPosition = transform.position;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public bool TryTakeContactDamage(int damage, Vector2 knockbackDirection, float knockbackSpeed)
    {
        if (IsResetting || !health.TryTakeDamage(damage))
            return false;

        if (knockbackDirection.sqrMagnitude < 0.0001f)
            knockbackDirection = Vector2.up;

        rb.linearVelocity = knockbackDirection.normalized * Mathf.Max(0f, knockbackSpeed);
        return true;
    }

    public bool BeginHazardReset(
        int damage,
        float resetDelay,
        float postResetInvulnerability,
        CameraRoomBounds room,
        Transform respawnPointOverride)
    {
        if (IsResetting)
            return false;

        Vector3 destination = startingPosition;
        if (respawnPointOverride != null)
            destination = respawnPointOverride.position;
        else if (room != null)
            destination = room.GetRespawnPosition(startingPosition);

        StartCoroutine(HazardResetRoutine(
            damage,
            Mathf.Max(0f, resetDelay),
            Mathf.Max(0f, postResetInvulnerability),
            destination));
        return true;
    }

    IEnumerator HazardResetRoutine(
        int damage,
        float resetDelay,
        float postResetInvulnerability,
        Vector3 destination)
    {
        IsResetting = true;
        health.TakeDamage(damage);

        if (controller != null)
            controller.SetControlsEnabled(false);

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        Sprite originalSprite = null;
        Color originalColor = Color.white;
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
            originalColor = spriteRenderer.color;

            if (hazardResetSprite != null)
                spriteRenderer.sprite = hazardResetSprite;

            spriteRenderer.color = hazardResetTint;
        }

        TryPlayHazardResetAnimation();

        float elapsed = 0f;
        while (elapsed < resetDelay)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                Color fadedColor = hazardResetTint;
                fadedColor.a = Mathf.Lerp(hazardResetTint.a, 0.1f, elapsed / Mathf.Max(resetDelay, 0.0001f));
                spriteRenderer.color = fadedColor;
            }

            yield return null;
        }

        destination.z = transform.position.z;
        transform.position = destination;
        rb.position = destination;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = true;
        Physics2D.SyncTransforms();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = originalSprite;
            spriteRenderer.color = originalColor;
        }

        health.GrantInvulnerability(postResetInvulnerability);

        if (controller != null)
            controller.SetControlsEnabled(true);

        CameraFollow cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (cameraFollow != null)
            cameraFollow.SnapToTarget();

        IsResetting = false;
    }

    void TryPlayHazardResetAnimation()
    {
        if (animator == null || string.IsNullOrWhiteSpace(hazardResetTrigger))
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger &&
                parameter.name == hazardResetTrigger)
            {
                animator.SetTrigger(hazardResetTrigger);
                return;
            }
        }
    }
}
