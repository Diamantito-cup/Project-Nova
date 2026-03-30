using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// TOKYO METROIDVANIA - PlayerHealth
/// Sistema de vida + Alma (soul) para curación, al estilo Hollow Knight.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Vida")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int currentHealth;

    [Header("Alma (Soul)")]
    [SerializeField] private int maxSoul = 99;
    [SerializeField] private int currentSoul = 0;
    [SerializeField] private int soulPerHit = 11;   // alma ganada al golpear
    [SerializeField] private int soulCostHeal = 33; // alma para curarse 1 vida

    [Header("Invencibilidad (i-frames)")]
    [SerializeField] private float iFramesDuration = 1.2f;
    [SerializeField] private float knockbackForce = 6f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 2.0f;
    [SerializeField] private Transform respawnPoint;

    [Header("Eventos")]
    public UnityEvent<int, int> OnHealthChanged;   // (current, max)
    public UnityEvent<int>      OnSoulChanged;     // (current)
    public UnityEvent           OnPlayerDeath;
    public UnityEvent           OnPlayerHeal;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────

    private bool isInvincible;
    private bool isDead;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr   = GetComponentInChildren<SpriteRenderer>();
        currentHealth = maxHealth;
    }

private void Update()
{
    var keyboard = UnityEngine.InputSystem.Keyboard.current;
    if (keyboard != null &&
        keyboard.xKey.wasPressedThisFrame &&
        keyboard.upArrowKey.isPressed &&
        currentSoul >= soulCostHeal &&
        !isDead)
    {
        TryHeal();
    }
}
    // ─────────────────────────────────────────────────────────────
    //  DAÑO
    // ─────────────────────────────────────────────────────────────

    public void TakeDamage(int amount, Vector3 sourcePosition)
    {
        if (isInvincible || isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(IFramesRoutine(sourcePosition));
    }

    private IEnumerator IFramesRoutine(Vector3 source)
    {
        isInvincible = true;
        anim.SetTrigger("Hit");

        // Knockback
        Vector2 dir = (transform.position - source).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(dir.x * knockbackForce, knockbackForce * 0.8f), ForceMode2D.Impulse);

        // Parpadeo neon (toggle alpha)
        float elapsed = 0f;
        while (elapsed < iFramesDuration)
        {
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.08f);
            elapsed += 0.08f;
        }
        sr.enabled = true;
        isInvincible = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  MUERTE
    // ─────────────────────────────────────────────────────────────

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        anim.SetTrigger("Death");
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;

        OnPlayerDeath?.Invoke();

        // Spawnear "Sombra" del jugador (como el shade de HK)
        // Instantiate(shadePrefab, transform.position, Quaternion.identity);

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Fade out → Fade in (manejado por UIManager)
        UIManager.Instance?.FadeIn();
        yield return new WaitForSeconds(0.5f);

        transform.position = respawnPoint != null
            ? respawnPoint.position
            : Vector3.zero;

        currentHealth = maxHealth;
        currentSoul   = 0;
        isDead = false;
        rb.isKinematic = false;
        anim.SetTrigger("Respawn");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnSoulChanged?.Invoke(currentSoul);

        UIManager.Instance?.FadeOut();
    }

    // ─────────────────────────────────────────────────────────────
    //  ALMA
    // ─────────────────────────────────────────────────────────────

    /// <summary>Llamado por PlayerCombat al golpear un enemigo.</summary>
    public void GainSoul(int amount = -1)
    {
        if (amount < 0) amount = soulPerHit;
        currentSoul = Mathf.Min(maxSoul, currentSoul + amount);
        OnSoulChanged?.Invoke(currentSoul);
    }

    private void TryHeal()
    {
        if (currentHealth >= maxHealth) return;

        currentSoul -= soulCostHeal;
        currentHealth = Mathf.Min(maxHealth, currentHealth + 1);

        anim.SetTrigger("Heal");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnSoulChanged?.Invoke(currentSoul);
        OnPlayerHeal?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────
    //  SETPOINT DE RESPAWN
    // ─────────────────────────────────────────────────────────────

    public void SetRespawnPoint(Transform point) => respawnPoint = point;

    // Propiedades públicas
    public int CurrentHealth => currentHealth;
    public int MaxHealth     => maxHealth;
    public int CurrentSoul   => currentSoul;
    public bool IsDead       => isDead;
    public void ConsumeSoul(int amount)
    {
        currentSoul = Mathf.Max(0, currentSoul - amount);
        OnSoulChanged?.Invoke(currentSoul);
    }

}