using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerAnimationDriver : MonoBehaviour
{
    private const string ControllerPath = "player"; // Resources/PlayerAnimator

    private static readonly int SpeedHash     = Animator.StringToHash("Speed");
    private static readonly int IsJumpingHash = Animator.StringToHash("isJumping");
    private static readonly int IsCrouchHash  = Animator.StringToHash("isCrouch");

    private Animator animator;
    private Rigidbody2D rb;
    private PlayerController player;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = gameObject.AddComponent<Animator>();

        if (animator.runtimeAnimatorController == null)
            animator.runtimeAnimatorController =
                Resources.Load<RuntimeAnimatorController>(ControllerPath);

        animator.applyRootMotion = false;
    }

    void Update()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        bool crouching = Keyboard.current != null && Keyboard.current.cKey.isPressed;

        animator.SetFloat(SpeedHash, Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool(IsJumpingHash, !player.isGrounded);
        animator.SetBool(IsCrouchHash, crouching && player.isGrounded);
    }
}