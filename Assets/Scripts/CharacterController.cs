using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.UI.Image;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public GameObject flarePrefab;
    public Transform firePoint;
    public float fireRate = 0.25f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.1f;
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
    private bool flarePressed;
    private bool firePressed;
    private bool aimHeld;
    private bool jetpackHeld;
    [SerializeField] private int fuel = 300;

    [SerializeField] private LightningBeam lightningBeam;

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

    public void OnFlare(InputValue value)
    {
        flarePressed = value.isPressed;
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
        /*Vector2 lightScreenPos = UnityEngine.Camera.main.WorldToScreenPoint(flashlight.transform.position);
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        Vector2 lookDirection = (mouseScreenPos - lightScreenPos).normalized;
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        flashlight.transform.rotation = Quaternion.Euler(0, 0, angle - 90);*/
    }
    public void OnJetpack(InputValue value)
    {
        jetpackHeld = value.isPressed;
    }

    void FixedUpdate()
    {
        HandleAim();
        HandleMovement();
        HandleJetpack();
        HandleJump();
        HandleFlare();
        HandleShooting();

        fireCooldown -= Time.deltaTime;

        // Reset the one-frame jump flag after it has been consumed
        firePressed = false;
        flarePressed = false;
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
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null ? true : false;

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
            //lightningBeam.StartFiring();
            fireCooldown = fireRate;
            // Spawn from firePoint if assigned, otherwise fall back to transform
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 spawnDir = moveInput != Vector2.zero ? moveInput : (facingLeft ? Vector3.left : Vector3.right);
            Instantiate(bulletPrefab, spawnPos, Quaternion.identity)
                .GetComponent<Bullet>().SetDirection(spawnDir);
            //AudioManager.Instance.PlayShoot();
        }
    }

    void HandleFlare()
    {
        if (flarePressed && fireCooldown <= 0f)
        {
            fireCooldown = fireRate * 2;
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 spawnDir = moveInput != Vector2.zero ? moveInput : (facingLeft ? Vector3.left : Vector3.right);
            Instantiate(flarePrefab, spawnPos, Quaternion.identity)
                .GetComponent<Flare>().SetDirection(spawnDir);
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