using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// TOKYO METROIDVANIA - PlayerAdvancedMovement
/// Habilidades especiales:
/// - Dash aéreo (C en el aire)
/// - Dash doble (C dos veces)
/// - Wall Slide (pegarse a la pared en el aire)
/// - Wall Jump (Z contra la pared)
/// 
/// Añadir al Player junto con PlayerController.
/// </summary>
public class PlayerAdvancedMovement : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Dash Aéreo / Doble Dash")]
    [SerializeField] private int maxAirDashes = 2;             // cuántos dashes en el aire
    [SerializeField] private float airDashForce = 20f;
    [SerializeField] private float airDashDuration = 0.15f;
    [SerializeField] private float airDashCooldown = 0.5f;
    [SerializeField] private GameObject airDashGhostPrefab;    // trail fantasma neon

    [Header("Wall Slide")]
    [SerializeField] private float wallSlideSpeed = 1.5f;      // velocidad de caída en pared
    [SerializeField] private Transform wallCheckRight;         // punto de detección pared derecha
    [SerializeField] private Transform wallCheckLeft;          // punto de detección pared izquierda
    [SerializeField] private float wallCheckDistance = 0.15f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpForceX = 10f;       // fuerza horizontal al saltar de pared
    [SerializeField] private float wallJumpForceY = 16f;       // fuerza vertical al saltar de pared
    [SerializeField] private float wallJumpLockTime = 0.2f;    // tiempo que se bloquea el input horizontal

    [Header("Efectos")]
    [SerializeField] private GameObject wallSlideFXPrefab;
    [SerializeField] private ParticleSystem dashTrail;

    // ─────────────────────────────────────────────────────────────
    //  PRIVADO
    // ─────────────────────────────────────────────────────────────

    private Rigidbody2D rb;
    private PlayerController playerController;
    private Animator anim;
    private CameraController cam;

    // Dash aéreo
    private int airDashesRemaining;
    private float airDashCooldownTimer;
    private bool isAirDashing;

    // Wall
    private bool isTouchingWallRight;
    private bool isTouchingWallLeft;
    private bool isWallSliding;
    private bool wallJumpLocked;
    private float wallJumpLockTimer;

    // Input
    private float inputX;
    private bool jumpPressed;

    private void Awake()
    {
        rb               = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        anim             = GetComponent<Animator>();
        cam              = Camera.main?.GetComponent<CameraController>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Leer input
        inputX = 0f;
        if (keyboard.leftArrowKey.isPressed)  inputX = -1f;
        if (keyboard.rightArrowKey.isPressed) inputX =  1f;
        jumpPressed = keyboard.zKey.wasPressedThisFrame;

        airDashCooldownTimer -= Time.deltaTime;

        // Resetear dashes al tocar el suelo
        if (playerController.IsGrounded)
            airDashesRemaining = maxAirDashes;

        CheckWalls();
        HandleWallSlide();
        HandleWallJump();
        HandleAirDash(keyboard);

        // Timer de wall jump lock
        if (wallJumpLocked)
        {
            wallJumpLockTimer -= Time.deltaTime;
            if (wallJumpLockTimer <= 0f)
                wallJumpLocked = false;
        }
    }

    private void FixedUpdate()
    {
        ApplyWallSlide();
    }

    // ─────────────────────────────────────────────────────────────
    //  DETECCIÓN DE PAREDES
    // ─────────────────────────────────────────────────────────────

    private void CheckWalls()
    {
        if (wallCheckRight != null)
            isTouchingWallRight = Physics2D.Raycast(
                wallCheckRight.position, Vector2.right, wallCheckDistance, wallLayer);

        if (wallCheckLeft != null)
            isTouchingWallLeft = Physics2D.Raycast(
                wallCheckLeft.position, Vector2.left, wallCheckDistance, wallLayer);
    }

    // ─────────────────────────────────────────────────────────────
    //  WALL SLIDE
    // ─────────────────────────────────────────────────────────────

    private void HandleWallSlide()
    {
        bool touchingWall = (isTouchingWallRight && inputX > 0f) ||
                            (isTouchingWallLeft  && inputX < 0f);

        isWallSliding = touchingWall &&
                        !playerController.IsGrounded &&
                        rb.linearVelocity.y < 0f;

        anim?.SetBool("IsWallSliding", isWallSliding);

        // Efecto de partículas
        if (dashTrail != null)
            dashTrail.gameObject.SetActive(isWallSliding);
    }

    private void ApplyWallSlide()
    {
        if (!isWallSliding) return;

        // Limitar velocidad de caída
        if (rb.linearVelocity.y < -wallSlideSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
    }

    // ─────────────────────────────────────────────────────────────
    //  WALL JUMP
    // ─────────────────────────────────────────────────────────────

    private void HandleWallJump()
    {
        if (!jumpPressed || !isWallSliding) return;

        // Dirección opuesta a la pared
        float jumpDir = isTouchingWallRight ? -1f : 1f;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(jumpDir * wallJumpForceX, wallJumpForceY), ForceMode2D.Impulse);

        // Bloquear input horizontal brevemente para que el salto se sienta limpio
        wallJumpLocked  = true;
        wallJumpLockTimer = wallJumpLockTime;

        // Resetear dashes al hacer wall jump
        airDashesRemaining = maxAirDashes;

        anim?.SetTrigger("Jump");
        cam?.ShakeCamera(0.04f, 0.08f);

        Debug.Log("[WallJump] Saltando de pared hacia: " + (jumpDir > 0 ? "derecha" : "izquierda"));
    }

    // ─────────────────────────────────────────────────────────────
    //  DASH AÉREO / DOBLE DASH
    // ─────────────────────────────────────────────────────────────

    private void HandleAirDash(Keyboard keyboard)
    {
        // Solo disponible en el aire
        if (playerController.IsGrounded) return;
        if (isAirDashing) return;
        if (airDashCooldownTimer > 0f) return;
        if (airDashesRemaining <= 0) return;

        if (keyboard.cKey.wasPressedThisFrame)
            StartCoroutine(AirDashRoutine());
    }

    private IEnumerator AirDashRoutine()
    {
        isAirDashing = true;
        airDashesRemaining--;
        airDashCooldownTimer = airDashCooldown;

        // Dirección del dash
        float dir = inputX != 0 ? Mathf.Sign(inputX) : (playerController.FacingRight ? 1f : -1f);

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dir * airDashForce, 0f);

        anim?.SetTrigger("Dash");

        // Trail de fantasmas neon
        float elapsed = 0f;
        while (elapsed < airDashDuration)
        {
            elapsed += Time.deltaTime;
            if (airDashGhostPrefab != null)
                Instantiate(airDashGhostPrefab, transform.position, Quaternion.identity);
            yield return null;
        }

        rb.gravityScale = originalGravity;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y);
        isAirDashing = false;

        Debug.Log("[AirDash] Dash aéreo! Restantes: " + airDashesRemaining);
    }

    // ─────────────────────────────────────────────────────────────
    //  PROPIEDADES PÚBLICAS
    // ─────────────────────────────────────────────────────────────

    public bool IsWallSliding => isWallSliding;
    public bool IsAirDashing  => isAirDashing;
    public bool IsTouchingWall => isTouchingWallRight || isTouchingWallLeft;

    // ─────────────────────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        if (wallCheckRight != null)
            Gizmos.DrawRay(wallCheckRight.position, Vector2.right * wallCheckDistance);
        if (wallCheckLeft != null)
            Gizmos.DrawRay(wallCheckLeft.position, Vector2.left * wallCheckDistance);
    }
}