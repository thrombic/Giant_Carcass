using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;

[RequireComponent(typeof(Rigidbody2D), typeof(HealthSystem), typeof(PlayerDamageReceiver))]
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
    [SerializeField] private GameObject jets;

    private Vector2 moveInput;
    private bool jumpHeld;
    private bool jumpPeaked;
    private bool firePressed;
    private bool aimHeld;
    private bool jetpackHeld;
    [SerializeField] private int fuel = 300;
    private bool controlsEnabled = true;

    [SerializeField] private LightningBeam lightningBeam;

    public bool ControlsEnabled => controlsEnabled;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (GetComponent<HealthSystem>() == null)
            gameObject.AddComponent<HealthSystem>();

        if (GetComponent<PlayerDamageReceiver>() == null)
            gameObject.AddComponent<PlayerDamageReceiver>();
    }

    // ?? Input System callbacks (wire these up in the Player Input component) ??
    // Set the Player Input component's Behavior to "Send Messages" and it will
    // call these automatically, or call them from an InputActionAsset directly.

    /// <summary>Called by PlayerInput when the Move action fires.</summary>
    public void OnMove(InputValue value)
    {
        moveInput = controlsEnabled ? value.Get<Vector2>() : Vector2.zero;
    }

    /// <summary>Called by PlayerInput when the Jump action fires.</summary>
    public void OnJump(InputValue value)
    {
        // GetButtonDown equivalent: only flag true on the press phase
        jumpHeld = controlsEnabled && value.isPressed;
    }

    /// <summary>Called by PlayerInput when the Fire action fires.</summary>
    public void OnFire(InputValue value)
    {
        // GetButton equivalent: track held state
        firePressed = controlsEnabled && value.isPressed;
    }

    public void OnAim(InputValue value)
    {
        aimHeld = controlsEnabled && value.isPressed;
    }

    public void OnLook(InputValue value)
    {
        /*Vector2 lightScreenPos = UnityEngine.Camera.main.WorldToScreenPoint(flashlight.transform.position);
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        Vector2 lookDirection = (mouseScreenPos - lightScreenPos).normalized;
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        flashlight.transform.rotation = Quaternion.Euler(0, 0, angle - 90);*/
    }
    public void OnJetpack(InputValue value)
    {
        jetpackHeld = controlsEnabled && value.isPressed;
    }

    void FixedUpdate()
    {
        if (!controlsEnabled)
            return;

        HandleAim();
        HandleMovement();
        HandleJetpack();
        HandleJump();
        HandleShooting();

        fireCooldown -= Time.deltaTime;

        // Reset the one-frame jump flag after it has been consumed
        firePressed = false;
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;

        if (enabled)
            return;

        moveInput = Vector2.zero;
        jumpHeld = false;
        firePressed = false;
        aimHeld = false;
        jetpackHeld = false;

        if (jets != null)
            jets.SetActive(false);

        if (lightningBeam != null)
            lightningBeam.StopFiring();
    }

    void HandleJetpack()
    {
        // while jetpacking, increase change in x and/or y velocity based on input, and reduce fuel
        if (jetpackHeld && moveInput != Vector2.zero && fuel > 0)
        {
            float angle = Mathf.Atan2(-moveInput.y, -moveInput.x) * Mathf.Rad2Deg;
            if (facingLeft)
                angle -= 180;

            jets.transform.rotation = Quaternion.Euler(0, 0, angle);
            jets.SetActive(true);

            rb.AddForce(moveInput * 15);
            fuel -= 1;
        }
        else
            jets.SetActive(false);
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
            if (jetpackHeld)
                rb.linearVelocityX = Mathf.Clamp(rb.linearVelocityX, -moveSpeed * 2, moveSpeed * 2);
            else
                rb.linearVelocityX = Mathf.Clamp(rb.linearVelocityX, -moveSpeed, moveSpeed);
        }
        else if (isGrounded)
        {
            rb.linearVelocityX *= 0.8f; // simple friction when no input
            fuel += 10;
            if (fuel > 300) fuel = 300;
        }
        //rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        if (moveInput.x > 0 && facingLeft) Flip();
        else if (moveInput.x < 0 && !facingLeft) Flip();

        HandleLight();
    }

    void HandleLight()
    {
        float angle = facingLeft ? 90: -90;
        if (moveInput != Vector2.zero)
        {
            angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
            flashlight.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }
        else
            flashlight.transform.rotation = Quaternion.Euler(0, 0, angle);
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
            lightningBeam.StartFiring();
            /*fireCooldown = fireRate;
            // Spawn from firePoint if assigned, otherwise fall back to transform
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 spawnDir = moveInput != Vector2.zero ? moveInput : (facingLeft ? Vector3.left : Vector3.right);
            Instantiate(bulletPrefab, spawnPos, Quaternion.identity)
                .GetComponent<Bullet>().SetDirection(spawnDir);
            //AudioManager.Instance.PlayShoot();*/
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
