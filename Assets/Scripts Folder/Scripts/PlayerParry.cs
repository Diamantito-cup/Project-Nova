using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// TOKYO METROIDVANIA - PlayerParry
/// Parry al estilo Hollow Knight / Silksong:
/// - Presiona Z dos veces rápido (doble tap) para activar el parry
/// - Ventana activa: 0.15 segundos
/// - Si un proyectil entra en el parry hitbox durante esa ventana → deflectado
/// - Recompensa: alma + efecto neon + breve invencibilidad
/// - Cooldown para evitar spam
/// </summary>
public class PlayerParry : MonoBehaviour
{
    [Header("Parry")]
    [SerializeField] private float parryWindow = 0.15f;       // duración del parry activo
    [SerializeField] private float parryCooldown = 0.8f;      // tiempo entre parries
    [SerializeField] private float parryRadius = 1.2f;        // radio del hitbox de parry
    [SerializeField] private LayerMask projectileLayer;        // capa de proyectiles enemigos
    [SerializeField] private LayerMask enemyLayer;             // para parry de ataques cuerpo a cuerpo

    [Header("Recompensas")]
    [SerializeField] private int soulOnParry = 33;             // alma ganada al parry exitoso
    [SerializeField] private float iFramesOnParry = 0.5f;     // invencibilidad tras parry

    [Header("Doble Tap")]
    [SerializeField] private float doubleTapWindow = 0.25f;   // ventana para detectar doble tap de Z

    [Header("Efectos")]
    [SerializeField] private GameObject parryFXPrefab;         // efecto visual neon
    [SerializeField] private GameObject parryActiveFXPrefab;   // efecto mientras parry está activo

    // Referencias
    private PlayerHealth playerHealth;
    private PlayerController playerController;
    private Animator anim;
    private CameraController cam;

    // Estado
    private bool isParrying;
    private float parryCooldownTimer;
    private float lastZPressTime = -10f;
    private GameObject activeParryFX;

    private void Awake()
    {
        playerHealth    = GetComponent<PlayerHealth>();
        playerController = GetComponent<PlayerController>();
        anim            = GetComponent<Animator>();
        cam             = Camera.main?.GetComponent<CameraController>();
    }

    private void Update()
    {
        parryCooldownTimer -= Time.deltaTime;

        var keyboard = Keyboard.current;
       if (keyboard.xKey.wasPressedThisFrame)
{
    float timeSinceLastX = Time.time - lastZPressTime;

    if (timeSinceLastX <= doubleTapWindow && !isParrying && parryCooldownTimer <= 0f)
    {
        StartCoroutine(ParryRoutine());
    }
    else
    {
        // Primer tap = ataque normal (manejado por PlayerCombat)
    }

    lastZPressTime = Time.time;
}
    }
    // ─────────────────────────────────────────────────────────────
    //  RUTINA DE PARRY
    // ─────────────────────────────────────────────────────────────

    private IEnumerator ParryRoutine()
    {
        isParrying = true;
        parryCooldownTimer = parryCooldown;

        // Efecto visual de parry activo
        if (parryActiveFXPrefab != null)
            activeParryFX = Instantiate(parryActiveFXPrefab, transform.position, Quaternion.identity, transform);

        anim?.SetTrigger("Parry");

        // Congelar al jugador brevemente
        var rb = GetComponent<Rigidbody2D>();
        Vector2 savedVelocity = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        float elapsed = 0f;
        bool parrySuccess = false;

        while (elapsed < parryWindow)
        {
            elapsed += Time.deltaTime;

            // Detectar proyectiles en el radio de parry
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, parryRadius, projectileLayer);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<ProjectileBase>(out var proj))
                {
                    OnParrySuccess(hit.gameObject);
                    parrySuccess = true;
                    break;
                }
            }

            // Detectar ataques cuerpo a cuerpo de enemigos
            if (!parrySuccess)
            {
                Collider2D[] enemyHits = Physics2D.OverlapCircleAll(transform.position, parryRadius * 0.7f, enemyLayer);
                foreach (var hit in enemyHits)
                {
                    if (hit.TryGetComponent<EnemyBase>(out _))
                    {
                        OnParrySuccess(null);
                        parrySuccess = true;
                        break;
                    }
                }
            }

            if (parrySuccess) break;
            yield return null;
        }

        // Restaurar física
        rb.gravityScale = 3.5f;
        rb.linearVelocity = savedVelocity;

        // Destruir efecto activo
        if (activeParryFX != null)
            Destroy(activeParryFX);

        if (!parrySuccess)
            OnParryFail();

        isParrying = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  PARRY EXITOSO
    // ─────────────────────────────────────────────────────────────

    private void OnParrySuccess(GameObject projectile)
    {
        // Destruir o deflectar el proyectil
        if (projectile != null)
        {
            // Deflectar: invertir velocidad del proyectil
            if (projectile.TryGetComponent<Rigidbody2D>(out var projRb))
            {
                projRb.linearVelocity = -projRb.linearVelocity * 1.5f; // rebota más rápido
                // Cambiar el layer del proyectil para que dañe enemigos
                projectile.layer = LayerMask.NameToLayer("PlayerProjectile");
            }
            else
            {
                Destroy(projectile);
            }
        }

        // Dar alma
        playerHealth?.GainSoul(soulOnParry);

        // I-frames
        StartCoroutine(ParryIFrames());

        // Screen shake suave
        cam?.ShakeCamera(0.05f, 0.1f);

        // Efecto visual neon
        if (parryFXPrefab != null)
            Instantiate(parryFXPrefab, transform.position, Quaternion.identity);

        // Freeze frame (pausa el tiempo brevemente para impacto visual)
        StartCoroutine(FreezeFrame(0.05f));

        anim?.SetTrigger("ParrySuccess");
        Debug.Log("[Parry] ¡Parry exitoso!");
    }

    // ─────────────────────────────────────────────────────────────
    //  PARRY FALLIDO
    // ─────────────────────────────────────────────────────────────

    private void OnParryFail()
    {
        // Pequeño efecto visual de fallo (sin penalización extra)
        anim?.SetTrigger("ParryFail");
        Debug.Log("[Parry] Parry fallido - ventana expirada");
    }

    // ─────────────────────────────────────────────────────────────
    //  I-FRAMES TRAS PARRY
    // ─────────────────────────────────────────────────────────────

    private IEnumerator ParryIFrames()
    {
        // Usar el sistema de i-frames del PlayerHealth
        // Por ahora simplemente marcamos invencibilidad temporal
        yield return new WaitForSeconds(iFramesOnParry);
    }

    // ─────────────────────────────────────────────────────────────
    //  FREEZE FRAME (efecto de impacto)
    // ─────────────────────────────────────────────────────────────

    private IEnumerator FreezeFrame(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    public bool IsParrying => isParrying;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, parryRadius);
        Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
        Gizmos.DrawSphere(transform.position, parryRadius);
    }
}