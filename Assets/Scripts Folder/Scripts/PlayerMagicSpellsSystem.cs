using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// TOKYO METROIDVANIA - PlayerMagic
/// Sistema de hechizos inspirado en Hollow Knight, estética Tokyo Neon:
///
/// HECHIZOS (todos cuestan 33 de alma):
/// ─────────────────────────────────────────────────────
/// [Arriba + A]    PULSO NEON         → proyectil de energía hacia arriba
///                 (con 66 alma) NOVA OSCURA → proyectil triple cargado
///
/// [Abajo + A]     IMPACTO SÍSMICO    → golpe al suelo, onda expansiva
///                 (con 66 alma) COLAPSO ORBITAL → onda doble + hoyo negro
///
/// [A sin dirección] GRITO DEL VACÍO  → explosión radial en todas las direcciones
/// ─────────────────────────────────────────────────────
/// </summary>
public class PlayerMagic : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Costos de Alma")]
    [SerializeField] private int soulCostBasic    = 33;
    [SerializeField] private int soulCostUpgraded = 66;

    [Header("Pulso Neon (Arriba + A)")]
    [SerializeField] private GameObject pulsoNeonPrefab;        // proyectil básico
    [SerializeField] private GameObject novaOscuraPrefab;       // proyectil evolucionado
    [SerializeField] private float pulsoSpeed = 18f;
    [SerializeField] private int pulsoDamage = 20;
    [SerializeField] private int novaDamage  = 35;

    [Header("Impacto Sísmico (Abajo + A)")]
    [SerializeField] private GameObject impactoFXPrefab;        // efecto visual en suelo
    [SerializeField] private GameObject colapsoFXPrefab;        // efecto evolucionado
    [SerializeField] private float impactoRadius = 3f;
    [SerializeField] private float colapsoRadius = 5f;
    [SerializeField] private int impactoDamage = 25;
    [SerializeField] private int colapsoDamage = 40;
    [SerializeField] private float impactoKnockback = 8f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Grito del Vacío (A sin dirección)")]
    [SerializeField] private GameObject gritoPrefab;            // onda radial
    [SerializeField] private float gritoRadius = 4f;
    [SerializeField] private int gritoDamage = 15;
    [SerializeField] private int gritoRayCount = 8;             // rayos en todas direcciones

    [Header("Casting")]
    [SerializeField] private float castLockDuration = 0.4f;     // bloqueo de movimiento al lanzar
    [SerializeField] private float spellCooldown = 0.3f;

    // ─────────────────────────────────────────────────────────────
    //  PRIVADO
    // ─────────────────────────────────────────────────────────────

    private PlayerHealth playerHealth;
    private PlayerController playerController;
    private Rigidbody2D rb;
    private Animator anim;
    private CameraController cam;

    private float spellCooldownTimer;
    private bool isCasting;

    private void Awake()
    {
        playerHealth     = GetComponent<PlayerHealth>();
        playerController = GetComponent<PlayerController>();
        rb               = GetComponent<Rigidbody2D>();
        anim             = GetComponent<Animator>();
        cam              = Camera.main?.GetComponent<CameraController>();
    }

    private void Update()
    {
        spellCooldownTimer -= Time.deltaTime;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (isCasting) return;
        if (spellCooldownTimer > 0f) return;

        // Leer dirección
        bool upHeld   = keyboard.upArrowKey.isPressed;
        bool downHeld = keyboard.downArrowKey.isPressed;
        bool aPressed = keyboard.aKey.wasPressedThisFrame;

        if (!aPressed) return;

        int soul = playerHealth.CurrentSoul;

        if (upHeld)
        {
            // PULSO NEON o NOVA OSCURA
            if (soul >= soulCostUpgraded)
                StartCoroutine(CastNovaOscura());
            else if (soul >= soulCostBasic)
                StartCoroutine(CastPulsoNeon());
            else
                NotEnoughSoul();
        }
        else if (downHeld)
        {
            // IMPACTO SÍSMICO o COLAPSO ORBITAL
            if (soul >= soulCostUpgraded)
                StartCoroutine(CastColapsoOrbital());
            else if (soul >= soulCostBasic)
                StartCoroutine(CastImpactoSismico());
            else
                NotEnoughSoul();
        }
        else
        {
            // GRITO DEL VACÍO
            if (soul >= soulCostBasic)
                StartCoroutine(CastGritoVacio());
            else
                NotEnoughSoul();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  PULSO NEON (básico)
    // ─────────────────────────────────────────────────────────────

    private IEnumerator CastPulsoNeon()
    {
        isCasting = true;
        ConsumeSoul(soulCostBasic);
        LockMovement(true);

        anim?.SetTrigger("CastUp");
        cam?.ShakeCamera(0.04f, 0.08f);

        // Disparar proyectil hacia arriba
        if (pulsoNeonPrefab != null)
        {
            GameObject proj = Instantiate(pulsoNeonPrefab,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity);

            if (proj.TryGetComponent<Rigidbody2D>(out var projRb))
                projRb.linearVelocity = Vector2.up * pulsoSpeed;

            if (proj.TryGetComponent<MagicProjectile>(out var mp))
                mp.SetDamage(pulsoDamage);

            Destroy(proj, 3f);
        }

        Debug.Log("[Magia] Pulso Neon lanzado!");

        yield return new WaitForSeconds(castLockDuration);
        LockMovement(false);
        spellCooldownTimer = spellCooldown;
        isCasting = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  NOVA OSCURA (evolucionado - triple disparo)
    // ─────────────────────────────────────────────────────────────

    private IEnumerator CastNovaOscura()
    {
        isCasting = true;
        ConsumeSoul(soulCostUpgraded);
        LockMovement(true);

        anim?.SetTrigger("CastUp");
        cam?.ShakeCamera(0.08f, 0.15f);

        // Triple proyectil: arriba, arriba-izquierda, arriba-derecha
        Vector2[] directions = {
            Vector2.up,
            new Vector2(-0.5f, 1f).normalized,
            new Vector2( 0.5f, 1f).normalized
        };

        GameObject prefab = novaOscuraPrefab != null ? novaOscuraPrefab : pulsoNeonPrefab;

        foreach (var dir in directions)
        {
            if (prefab != null)
            {
                GameObject proj = Instantiate(prefab,
                    transform.position + Vector3.up * 0.5f,
                    Quaternion.identity);

                if (proj.TryGetComponent<Rigidbody2D>(out var projRb))
                    projRb.linearVelocity = dir * pulsoSpeed * 1.3f;

                if (proj.TryGetComponent<MagicProjectile>(out var mp))
                    mp.SetDamage(novaDamage);

                Destroy(proj, 3f);
            }
        }

        // Freeze frame dramático
        StartCoroutine(FreezeFrame(0.06f));
        Debug.Log("[Magia] ¡Nova Oscura lanzada!");

        yield return new WaitForSeconds(castLockDuration * 1.2f);
        LockMovement(false);
        spellCooldownTimer = spellCooldown;
        isCasting = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  IMPACTO SÍSMICO (básico)
    // ─────────────────────────────────────────────────────────────

    private IEnumerator CastImpactoSismico()
    {
        isCasting = true;
        ConsumeSoul(soulCostBasic);
        LockMovement(true);

        anim?.SetTrigger("CastDown");

        // Solo funciona en el suelo
        if (!playerController.IsGrounded)
        {
            // Caer rápido al suelo primero
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -20f);
            yield return new WaitUntil(() => playerController.IsGrounded);
        }

        // Onda expansiva al aterrizar
        cam?.ShakeCamera(0.15f, 0.25f);
        StartCoroutine(FreezeFrame(0.05f));

        // Efecto visual
        if (impactoFXPrefab != null)
            Instantiate(impactoFXPrefab, transform.position, Quaternion.identity);

        // Dañar enemigos en radio
        DamageInRadius(transform.position, impactoRadius, impactoDamage, impactoKnockback);

        Debug.Log("[Magia] Impacto Sísmico lanzado!");

        yield return new WaitForSeconds(castLockDuration);
        LockMovement(false);
        spellCooldownTimer = spellCooldown;
        isCasting = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  COLAPSO ORBITAL (evolucionado - onda doble + hoyo negro)
    // ─────────────────────────────────────────────────────────────

    private IEnumerator CastColapsoOrbital()
    {
        isCasting = true;
        ConsumeSoul(soulCostUpgraded);
        LockMovement(true);

        anim?.SetTrigger("CastDown");

        if (!playerController.IsGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -25f);
            yield return new WaitUntil(() => playerController.IsGrounded);
        }

        // Primera onda
        cam?.ShakeCamera(0.2f, 0.3f);
        StartCoroutine(FreezeFrame(0.08f));

        if (colapsoFXPrefab != null)
            Instantiate(colapsoFXPrefab, transform.position, Quaternion.identity);

        DamageInRadius(transform.position, colapsoRadius * 0.6f, colapsoDamage, impactoKnockback * 1.5f);

        yield return new WaitForSeconds(0.15f);

        // Segunda onda (más grande)
        cam?.ShakeCamera(0.25f, 0.3f);
        DamageInRadius(transform.position, colapsoRadius, colapsoDamage / 2, impactoKnockback);

        if (colapsoFXPrefab != null)
        {
            GameObject fx2 = Instantiate(colapsoFXPrefab, transform.position, Quaternion.identity);
            fx2.transform.localScale *= 1.8f;
        }

        Debug.Log("[Magia] ¡Colapso Orbital lanzado!");

        yield return new WaitForSeconds(castLockDuration * 1.5f);
        LockMovement(false);
        spellCooldownTimer = spellCooldown;
        isCasting = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  GRITO DEL VACÍO (explosión radial)
    // ─────────────────────────────────────────────────────────────

    private IEnumerator CastGritoVacio()
    {
        isCasting = true;
        ConsumeSoul(soulCostBasic);
        LockMovement(true);

        anim?.SetTrigger("CastNeutral");
        cam?.ShakeCamera(0.12f, 0.2f);
        StartCoroutine(FreezeFrame(0.06f));

        // Efecto visual radial
        if (gritoPrefab != null)
            Instantiate(gritoPrefab, transform.position, Quaternion.identity);

        // Disparar rayos en todas las direcciones
        float angleStep = 360f / gritoRayCount;
        for (int i = 0; i < gritoRayCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // Raycast para dañar enemigos en cada dirección
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, gritoRadius, enemyLayer);
            if (hit.collider != null && hit.collider.TryGetComponent<EnemyHealth>(out var enemy))
                enemy.TakeDamage(gritoDamage, transform.position);
        }

        // También dañar en área
        DamageInRadius(transform.position, gritoRadius * 0.5f, gritoDamage, 5f);

        Debug.Log("[Magia] ¡Grito del Vacío Digital!");

        yield return new WaitForSeconds(castLockDuration);
        LockMovement(false);
        spellCooldownTimer = spellCooldown;
        isCasting = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────

    private void DamageInRadius(Vector3 center, float radius, int damage, float knockback)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<EnemyHealth>(out var enemy))
            {
                enemy.TakeDamage(damage, center);

                // Knockback
                if (hit.TryGetComponent<Rigidbody2D>(out var enemyRb))
                {
                    Vector2 dir = ((Vector2)(hit.transform.position - center)).normalized;
                    enemyRb.AddForce(dir * knockback, ForceMode2D.Impulse);
                }
            }
        }
    }

    private void ConsumeSoul(int amount)
    {
        // Usar reflexión para acceder al campo privado de PlayerHealth
        // O mejor: añadir un método público ConsumeSoul en PlayerHealth
        Debug.Log($"[Magia] Consumiendo {amount} de alma");
        // playerHealth.ConsumeSoul(amount); // descomentar cuando añadas el método
    }

    private void LockMovement(bool locked)
    {
        // Pausar el movimiento horizontal durante el cast
        if (locked)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private IEnumerator FreezeFrame(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    private void NotEnoughSoul()
    {
        Debug.Log("[Magia] No hay suficiente alma!");
        // Aquí puedes añadir un efecto visual/sonoro de "sin alma"
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, impactoRadius);
        Gizmos.color = new Color(1f, 0f, 0.5f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, gritoRadius);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  MagicProjectile — componente del proyectil mágico
// ─────────────────────────────────────────────────────────────────────────────
public class MagicProjectile : MonoBehaviour
{
    private int damage = 20;

    public void SetDamage(int d) => damage = d;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.TryGetComponent<EnemyHealth>(out var enemy))
        {
            enemy.TakeDamage(damage, transform.position);
            Destroy(gameObject);
        }
        else if (col.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}