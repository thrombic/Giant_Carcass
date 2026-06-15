using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;

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
    private bool facingLeft = true;

    // ?? New Input System: cached input values read from callbacks ??
    [SerializeField] private GameObject flashlight;
    private Vector2 moveInput;
    private Vector2 lookDirection = new Vector2(1, 0);
    private bool jumpHeld;
    private bool jumpPeaked;
    private bool firePressed;
    private bool aimHeld;

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
        jumpHeld = value.isPressed;
    }

    /// <summary>Called by PlayerInput when the Fire action fires.</summary>
    public void OnFire(InputValue value)
    {
        // GetButton equivalent: track held state
        firePressed = value.isPressed;
    }

    public void OnAim(InputValue value)
    {
        aimHeld = value.isPressed;
    }

    public void OnLook(InputValue value)
    {
        Vector2 lightScreenPos = UnityEngine.Camera.main.WorldToScreenPoint(flashlight.transform.position);
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        Vector2 lookDirection = (mouseScreenPos - lightScreenPos).normalized;
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        flashlight.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    void FixedUpdate()
    {
        HandleAim();
        HandleMovement();
        
        HandleJump();
        HandleShooting();

        fireCooldown -= Time.deltaTime;

        // Reset the one-frame jump flag after it has been consumed
        firePressed = false;
    }

    void HandleAim()
    {
        if (aimHeld && isGrounded)
        {
            if (moveInput.x > 0 && facingLeft) Flip();
            else if (moveInput.x < 0 && !facingLeft) Flip();

            // TODO: gun and eyes should follow aim direction

        }
    }

    void HandleMovement()
    {
        // moveInput.x replaces Input.GetAxisRaw("Horizontal")
        if (!aimHeld && moveInput.x != 0)
        {
            rb.linearVelocityX += moveInput.x;
            rb.linearVelocityX = Mathf.Clamp(rb.linearVelocityX, -moveSpeed, moveSpeed);
        }
        else
        {
            rb.linearVelocityX *= 0.8f; // simple friction when no input
        }
        //rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        if (moveInput.x > 0 && facingLeft) Flip();
        else if (moveInput.x < 0 && !facingLeft) Flip();
    }

    void HandleJump()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (jumpHeld && isGrounded)
        {
            jumpPeaked = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            //AudioManager.Instance.PlayJump();
        }
        if (!jumpPeaked)
        {
            if (!jumpHeld && rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                jumpPeaked = true;
            }
        }
    }

    void HandleShooting()
    {
        if (firePressed && fireCooldown <= 0f)
        {
            fireCooldown = fireRate;
            // Spawn from firePoint if assigned, otherwise fall back to transform
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Instantiate(bulletPrefab, spawnPos, Quaternion.identity)
                .GetComponent<Bullet>().SetDirection(facingLeft ? Vector2.left : Vector2.right);
            //AudioManager.Instance.PlayShoot();
        }
    }

    void Flip()
    {
        facingLeft = !facingLeft;
        transform.localScale = new Vector3(
            -transform.localScale.x,
            transform.localScale.y,
            transform.localScale.z);
    }
}