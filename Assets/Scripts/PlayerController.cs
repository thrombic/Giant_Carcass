using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.UI.Image;

[RequireComponent(typeof(Rigidbody2D), typeof(HealthSystem), typeof(PlayerDamageReceiver))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 12f;
    public float jumpForce = 18f;
    public float jetpackForce = 12.5f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public GameObject flarePrefab;
    public Transform firePoint;
    public float fireRate = 0.25f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer;

    [Header("Slope Handling")]
    [Tooltip("Max angle (degrees) considered walkable ground vs. a wall.")]
    public float maxSlopeAngle = 60f;
    [Tooltip("Extra downward push while grounded on a slope, keeps the player from bouncing on descents.")]
    public float slopeStickForce = 8f;

    // Raycast-based grounding (fast, predictive, decides isGrounded)
    private Vector2 raycastNormal = Vector2.up;

    private ContactPoint2D[] contactBuffer = new ContactPoint2D[8];
    private Vector2 contactNormal = Vector2.up;
    private bool hasFreshContact = false;
    private int framesSinceLastContact = 0;

    // Slope state, updated each ground check in HandleJump()
    private Vector2 groundNormal = Vector2.up;
    private float currentSlopeAngle;
    private bool isGrounded;
    private bool onSlope;

    private Rigidbody2D rb;
    private float fireCooldown;
    private bool facingLeft = true;

    // ?? New Input System: cached input values read from callbacks ??
    [SerializeField] private GameObject flashlight;
    [SerializeField] private GameObject jets;

    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool jumpPeaked;
    private bool flarePressed;
    private bool firePressed;
    private bool aimHeld;
    private bool jetpackHeld;

    public bool isStunned = false;

    private SpriteRenderer spriteRenderer;

    [SerializeField] private int fuel = 90;
    [SerializeField] private int maxFuel = 90;

    [SerializeField] private LightningBeam lightningBeam;
    
    private bool controlsEnabled = true;
    public bool ControlsEnabled => controlsEnabled;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (GetComponent<HealthSystem>() == null)
            gameObject.AddComponent<HealthSystem>();

        if (GetComponent<PlayerDamageReceiver>() == null)
            gameObject.AddComponent<PlayerDamageReceiver>();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) == 0)
            return;

        int count = collision.GetContacts(contactBuffer);
        float bestDot = -1f;
        Vector2 bestNormal = Vector2.zero;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            Vector2 normal = contactBuffer[i].normal;
            float dot = Vector2.Dot(normal, Vector2.up);

            // Ignore contacts that are basically walls (near-horizontal normal)
            if (dot > bestDot && dot > 0.1f)
            {
                bestDot = dot;
                bestNormal = normal;
                found = true;
            }
        }

        if (found)
        {
            contactNormal = bestNormal;
            framesSinceLastContact = 0;
        }
    }

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
        jumpPressed = jumpHeld = !isStunned && value.isPressed;
    }

    public void OnFlare(InputValue value)
    {
        flarePressed = !isStunned && value.isPressed;
    }

    /// <summary>Called by PlayerInput when the Fire action fires.</summary>
    public void OnFire(InputValue value)
    {
        // GetButton equivalent: track held state
        firePressed = !isStunned && value.isPressed;
    }

    public void OnAim(InputValue value)
    {
        aimHeld = !isStunned && value.isPressed;
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
        jetpackHeld = !isStunned && value.isPressed;
    }

    void FixedUpdate()
    {
        //if (isStunned) return;

        HandleAim();
        HandleGroundAndSlope();

        if (!isStunned)
            HandleMovement();

        HandleJetpack();
        HandleJump();
        HandleFlare();
        HandleShooting();

        fireCooldown -= Time.deltaTime;

        // Reset the one-frame jump flag after it has been consumed
        jumpPressed = false;
        firePressed = false;
        flarePressed = false;
    }

    /// Casts the same left/right ground rays used for isGrounded, but also
    /// reads the surface normal so movement/jumping can account for slopes.
    /// Runs once per FixedUpdate, before movement and jump logic use the result.
    /// </summary>
    void HandleGroundAndSlope()
    {
        float dynamicCheckDistance = groundCheckDistance + Mathf.Abs(rb.linearVelocity.x) * Time.fixedDeltaTime;

        RaycastHit2D hitLeft = Physics2D.Raycast(groundCheck.position - new Vector3(1.5f, 0, 0), Vector2.down, dynamicCheckDistance, groundLayer); // TODO: change this to use collider bounds
        RaycastHit2D hitRight = Physics2D.Raycast(groundCheck.position + new Vector3(1.5f, 0, 0), Vector2.down, dynamicCheckDistance, groundLayer);

        bool hitAny = hitLeft.collider != null || hitRight.collider != null;

        if (hitAny)
        {
            // Prefer the closer hit's normal; if both hit, average them for a
            // smoother result across tile/curve boundaries.
            Vector2 normal;
            if (hitLeft.collider != null && hitRight.collider != null)
                normal = (hitLeft.normal + hitRight.normal).normalized;
            else
                normal = hitLeft.collider != null ? hitLeft.normal : hitRight.normal;

            raycastNormal = normal;
            float raycastAngle = Vector2.Angle(raycastNormal, Vector2.up);
            isGrounded = raycastAngle <= maxSlopeAngle;
        }
        else
        {
            isGrounded = false;
        }

        // --- Pick which normal to trust ---
        // Contacts are more accurate but a frame stale; only trust them if they
        // arrived recently (last 1-2 physics steps) so a contact from an old
        // collision doesn't linger after the player has left the ground.
        if (isGrounded && hasFreshContact && framesSinceLastContact <= 1)
        {
            groundNormal = contactNormal;
        }
        else if (isGrounded)
        {
            groundNormal = raycastNormal;
        }
        else
        {
            groundNormal = Vector2.up;
        }

        currentSlopeAngle = isGrounded ? Vector2.Angle(groundNormal, Vector2.up) : 0f;
        onSlope = isGrounded && currentSlopeAngle > 2f;

        // Age out the contact data; OnCollisionStay2D will refresh this to 0
        // if we're still actually touching something this physics step.
        framesSinceLastContact++;
        hasFreshContact = framesSinceLastContact <= 1;
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;

        if (enabled)
            return;

        jumpHeld = false;
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

            rb.AddForce(moveInput * jetpackForce);
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
            float maxSpeed = (jetpackHeld && fuel > 0) ? moveSpeed * 2 : moveSpeed;

            if (isGrounded && onSlope && !jumpHeld)
            {
                // Redirect horizontal input along the slope surface so the
                // player accelerates parallel to the ground instead of
                // straight sideways, which would fight the collider on
                // steeper curves/ramps.
                Vector2 slopeDir = new Vector2(groundNormal.y, -groundNormal.x);
                Vector2 alongSlope = slopeDir * moveInput.x;

                rb.linearVelocity = alongSlope * maxSpeed;

                // Keep the player stuck to descending slopes instead of momentarily
                // going airborne over convex bumps.
                if (rb.linearVelocity.y <= 0f)
                    rb.linearVelocityY -= slopeStickForce * Time.fixedDeltaTime;

            }
            else
            {
                rb.linearVelocityX += moveInput.x;
                rb.linearVelocityX = Mathf.Clamp(rb.linearVelocityX, -maxSpeed, maxSpeed);
            }
        }
        else if (isGrounded)
        {
            rb.linearVelocityX *= 0.8f; // simple friction when no input
            rb.linearVelocityY *= 0.8f;
            fuel += 5; // TODO: perhaps fuel should regen whenever grounded
            if (fuel > maxFuel) fuel = maxFuel;
        }

        if (isGrounded && !onSlope && !jumpHeld && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocityY = 0;
        }

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
        // Ground/slope state is now computed once per frame in HandleGroundAndSlope(),
        // called earlier in FixedUpdate, so isGrounded is already up to date here.

        // Blend jump direction toward the ground normal so jumping off a
        // slope gives a natural push instead of always firing straight up.
        //Vector2 jumpDir = onSlope ? Vector2.Lerp(Vector2.up, groundNormal, 0.5f).normalized : Vector2.up; 

        if (jumpPressed && isGrounded)
        {
            jumpPeaked = false;

            Vector2 jumpDir = Vector2.up;
            Vector2 launchVelocity = jumpDir * jumpForce;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x + launchVelocity.x, launchVelocity.y);
            //AudioManager.Instance.PlayJump();
        }
        if (!jumpPeaked)
        {
            if (!jumpHeld && rb.linearVelocity.y > 0)
            {
                rb.linearVelocityY = 0;
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
