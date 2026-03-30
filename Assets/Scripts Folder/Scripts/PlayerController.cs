using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// TOKYO METROIDVANIA - PlayerController (Unity 6 - New Input System)
/// Z = Saltar / Doble Salto
/// X = Atacar
/// C = Dash
/// Flechas = Moverse
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 16f;
    [SerializeField] private float airControlMultiplier = 0.7f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float jumpCutMultiplier = 0.4f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float fallMultiplier = 2.8f;
    [SerializeField] private float lowJumpMultiplier = 2.0f;
    [SerializeField] private int maxJumps = 2;

    [Header("Dash")]
    [SerializeField] private float dashForce = 22f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.6f;

    [Header("Detección de Suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.4f, 0.05f);
    [SerializeField] private LayerMask groundLayer;

    // ── Componentes ──
    private Rigidbody2D rb;
    private Animator anim;
    private PlayerCombat combat;

    // ── Input ──
    private Vector2 moveInput;
    private bool jumpHeld;

    // ── Estado ──
    private bool isGrounded;
    private bool wasGrounded;
    private bool facingRight = true;
    private bool isDashing;
    private bool canDash = true;
    private bool isAttacking;

    private int jumpsRemaining;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float dashCooldownTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        combat = GetComponent<PlayerCombat>();
    }

    private void Update()
    {
        // ── Leer input con New Input System ──
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            Debug.Log(keyboard is null);
            return;
        }

           if (keyboard.zKey.wasPressedThisFrame)
{
    Debug.Log("Z presionado! JumpBuffer: " + jumpBufferTimer + " Grounded: " + isGrounded + " CoyoteTimer: " + coyoteTimer);
    jumpBufferTimer = jumpBufferTime;
}

        // Movimiento con flechas
        moveInput.x = 0f;
        if (keyboard.leftArrowKey.isPressed)  moveInput.x = -1f;
        if (keyboard.rightArrowKey.isPressed) moveInput.x =  1f;
        moveInput.y = 0f;
        if (keyboard.upArrowKey.isPressed)   moveInput.y =  1f;
        if (keyboard.downArrowKey.isPressed) moveInput.y = -1f;

        // Jump buffer
        if (keyboard.zKey.wasPressedThisFrame)
            jumpBufferTimer = jumpBufferTime;

        // Mantener Z para salto variable
        jumpHeld = keyboard.zKey.isPressed;

       // Dash
if (keyboard.cKey.wasPressedThisFrame)
{
    Debug.Log("C presionado! canDash: " + canDash + " cooldown: " + dashCooldownTimer + " isDashing: " + isDashing);
    if (canDash && dashCooldownTimer <= 0f && !isDashing)
        StartCoroutine(DashRoutine());
}
        // Ataque
        if (keyboard.xKey.wasPressedThisFrame)
            combat?.TriggerAttack(moveInput);

        CheckGround();
        HandleCoyoteTime();
        HandleJumpBuffer();
        HandleDash();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (!isDashing)
        {
            HandleMovement();
            ApplyBetterGravity();
        }
    }

    // ─── SUELO ───
    private void CheckGround()
{
    wasGrounded = isGrounded;
    isGrounded = Physics2D.OverlapBox(
        groundCheck.position, groundCheckSize, 0f, groundLayer);

    if (isGrounded && !wasGrounded)
    {
        jumpsRemaining = maxJumps;
        canDash = true;
        anim.SetTrigger("Land");
    }

    // Resetear dash mientras está en el suelo
    if (isGrounded) canDash = true;
}

    // ─── COYOTE TIME ───
    private void HandleCoyoteTime()
    {
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;
    }

    // ─── JUMP BUFFER ───
    private void HandleJumpBuffer()
    {
        jumpBufferTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f)
        {
            bool canJumpFromGround = coyoteTimer > 0f;
            bool canDoubleJump = !canJumpFromGround && jumpsRemaining > 0;

            if (canJumpFromGround || canDoubleJump)
                ExecuteJump(canJumpFromGround);
        }

        // Cortar salto al soltar Z
        if (!jumpHeld && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier);
    }

    private void ExecuteJump(bool fromGround)
    {
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        if (!fromGround)
        {
            jumpsRemaining--;
            anim.SetTrigger("DoubleJump");
        }
        else
        {
            jumpsRemaining = maxJumps - 1;
            anim.SetTrigger("Jump");
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    // ─── DASH ───
    private void HandleDash()
    {
        dashCooldownTimer -= Time.deltaTime;
    }

    private System.Collections.IEnumerator DashRoutine()
    {
        isDashing = true;
        canDash = false;
        dashCooldownTimer = dashCooldown;

        float dir = moveInput.x != 0 ? Mathf.Sign(moveInput.x) : (facingRight ? 1f : -1f);
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dir * dashForce, 0f);

        anim.SetTrigger("Dash");

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.2f, rb.linearVelocity.y);
        isDashing = false;
    }

    // ─── MOVIMIENTO ───
    private void HandleMovement()
    {
        float targetSpeed = moveInput.x * moveSpeed;
        float accel = isGrounded ? acceleration : acceleration * airControlMultiplier;
        float decel = isGrounded ? deceleration : deceleration * airControlMultiplier;

        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float appliedAccel = Mathf.Abs(targetSpeed) > 0.01f ? accel : decel;

        rb.AddForce(new Vector2(speedDiff * appliedAccel, 0f), ForceMode2D.Force);

        if (moveInput.x > 0f && !facingRight) Flip();
        else if (moveInput.x < 0f && facingRight) Flip();
    }

    // ─── GRAVEDAD MEJORADA ───
    private void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
    }

    // ─── ANIMATOR ───
    private void UpdateAnimator()
    {
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("SpeedX", Mathf.Abs(rb.linearVelocity.x));
        anim.SetFloat("SpeedY", rb.linearVelocity.y);
        anim.SetBool("IsDashing", isDashing);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    public void SetAttacking(bool value) => isAttacking = value;
    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public float InputX => moveInput.x;
    public float InputY => moveInput.y;
    public bool FacingRight => facingRight;

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}