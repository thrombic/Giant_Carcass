using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.25f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;
    private float fireCooldown;
    private bool facingRight = true;

    // ?? New Input System: cached input values read from callbacks ??
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool fireHeld;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    // ?? Input System callbacks (wire these up in the Player Input component) ??
    // Set the Player Input component's Behavior to "Send Messages" and it will
    // call these automatically, or call them from an InputActionAsset directly.

    /// <summary>Called by PlayerInput when the Move action fires.</summary>
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    /// <summary>Called by PlayerInput when the Jump action fires.</summary>
    public void OnJump(InputValue value)
    {
        // GetButtonDown equivalent: only flag true on the press phase
        if (value.isPressed)
            jumpPressed = true;
    }

    /// <summary>Called by PlayerInput when the Fire action fires.</summary>
    public void OnFire(InputValue value)
    {
        // GetButton equivalent: track held state
        fireHeld = value.isPressed;
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleShooting();

        fireCooldown -= Time.deltaTime;

        // Reset the one-frame jump flag after it has been consumed
        jumpPressed = false;
    }

    void HandleMovement()
    {
        // moveInput.x replaces Input.GetAxisRaw("Horizontal")
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        if (moveInput.x > 0 && !facingRight) Flip();
        else if (moveInput.x < 0 && facingRight) Flip();
    }

    void HandleJump()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            //AudioManager.Instance.PlayJump();
        }
    }

    void HandleShooting()
    {
        if (fireHeld && fireCooldown <= 0f)
        {
            fireCooldown = fireRate;
            // Spawn from firePoint if assigned, otherwise fall back to transform
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Instantiate(bulletPrefab, spawnPos, Quaternion.identity)
                .GetComponent<Bullet>().SetDirection(facingRight ? Vector2.right : Vector2.left);
            //AudioManager.Instance.PlayShoot();
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        transform.localScale = new Vector3(
            -transform.localScale.x,
            transform.localScale.y,
            transform.localScale.z);
    }
}