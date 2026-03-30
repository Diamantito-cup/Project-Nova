using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// TOKYO METROIDVANIA - PlayerPogo
/// Pogo al estilo Hollow Knight:
/// - Atacar hacia abajo en el aire (flecha abajo + X) sobre un enemigo o proyectil
/// - Rebota al jugador hacia arriba
/// - Se puede hacer infinitamente mientras haya enemigos debajo
/// - Daña al enemigo con cada rebote
/// </summary>
public class PlayerPogo : MonoBehaviour
{
    [Header("Pogo")]
    [SerializeField] private float pogoBounceForce = 18f;      // fuerza del rebote
    [SerializeField] private float pogoDownAttackRadius = 0.8f; // radio de detección abajo
    [SerializeField] private int pogoDamage = 15;              // daño del pogo
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask projectileLayer;

    [Header("Feel")]
    [SerializeField] private float freezeFrameDuration = 0.04f; // freeze frame al impactar
    [SerializeField] private GameObject pogoFXPrefab;           // efecto visual neon

    // Referencias
    private Rigidbody2D rb;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private CameraController cam;
    private Animator anim;

    // Estado
    private bool canPogo = true;
    private float pogoCooldown = 0.1f;
    private float pogoCooldownTimer;

    private void Awake()
    {
        rb               = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        playerHealth     = GetComponent<PlayerHealth>();
        anim             = GetComponent<Animator>();
        cam              = Camera.main?.GetComponent<CameraController>();
    }

    private void Update()
    {
        pogoCooldownTimer -= Time.deltaTime;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Pogo: flecha abajo + X en el aire
        bool downHeld = keyboard.downArrowKey.isPressed;
        bool attackPressed = keyboard.xKey.wasPressedThisFrame;
        bool inAir = !playerController.IsGrounded;

        if (downHeld && attackPressed && inAir && pogoCooldownTimer <= 0f)
        {
            TryPogo();
        }

        // Resetear pogo al tocar el suelo
        if (playerController.IsGrounded)
            canPogo = true;
    }

    // ─────────────────────────────────────────────────────────────
    //  INTENTAR POGO
    // ─────────────────────────────────────────────────────────────

    private void TryPogo()
    {
        // Buscar enemigos o proyectiles debajo del jugador
        Vector2 checkPos = (Vector2)transform.position + Vector2.down * 0.5f;

        // Verificar enemigos
        Collider2D[] enemyHits = Physics2D.OverlapCircleAll(checkPos, pogoDownAttackRadius, enemyLayer);
        foreach (var hit in enemyHits)
        {
            if (hit.TryGetComponent<EnemyHealth>(out var enemy))
            {
                ExecutePogo(hit.transform.position, enemy, null);
                return;
            }
        }

        // Verificar proyectiles
        Collider2D[] projHits = Physics2D.OverlapCircleAll(checkPos, pogoDownAttackRadius, projectileLayer);
        foreach (var hit in projHits)
        {
            if (hit.TryGetComponent<ProjectileBase>(out var proj))
            {
                ExecutePogo(hit.transform.position, null, proj);
                return;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  EJECUTAR POGO
    // ─────────────────────────────────────────────────────────────

    private void ExecutePogo(Vector3 hitPos, EnemyHealth enemy, ProjectileBase projectile)
    {
        pogoCooldownTimer = pogoCooldown;

        // Dañar enemigo
        enemy?.TakeDamage(pogoDamage, transform.position);

        // Destruir proyectil si es pogo sobre proyectil
        if (projectile != null)
            Destroy(projectile.gameObject);

        // Dar alma al jugador
        playerHealth?.GainSoul(11);

        // ── REBOTE ──
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * pogoBounceForce, ForceMode2D.Impulse);

        // Efecto visual
        if (pogoFXPrefab != null)
            Instantiate(pogoFXPrefab, hitPos, Quaternion.identity);

        // Screen shake suave
        cam?.ShakeCamera(0.06f, 0.08f);

        // Freeze frame
        StartCoroutine(FreezeFrame());

        anim?.SetTrigger("AttackDown");

        Debug.Log("[Pogo] ¡Rebote exitoso!");
    }

    // ─────────────────────────────────────────────────────────────
    //  FREEZE FRAME
    // ─────────────────────────────────────────────────────────────

    private IEnumerator FreezeFrame()
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(freezeFrameDuration);
        Time.timeScale = 1f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Vector2 checkPos = (Vector2)transform.position + Vector2.down * 0.5f;
        Gizmos.DrawWireSphere(checkPos, pogoDownAttackRadius);
    }
}