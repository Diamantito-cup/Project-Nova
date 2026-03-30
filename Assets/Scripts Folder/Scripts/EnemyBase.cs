using UnityEngine;
using System.Collections;

/// <summary>
/// TOKYO METROIDVANIA - EnemyBase (clase base para todos los enemigos)
/// Enemigos estilo semi-futuristas de Tokio: drones, samurais-cyborg, etc.
/// FSM simple: Patrol → Chase → Attack → Stunned
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBase : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Estadísticas")]
    [SerializeField] protected int maxHealth = 30;
    [SerializeField] protected int contactDamage = 10;
    [SerializeField] protected int soulsOnDeath = 22; // alma que da al morir

    [Header("Movimiento")]
    [SerializeField] protected float patrolSpeed = 2.5f;
    [SerializeField] protected float chaseSpeed  = 5f;
    [SerializeField] protected float patrolDistance = 5f;

    [Header("Detección")]
    [SerializeField] protected float detectionRange = 8f;
    [SerializeField] protected float attackRange    = 1.2f;
    [SerializeField] protected float losAngle = 60f; // ángulo de visión

    [Header("Stun")]
    [SerializeField] protected float stunDuration = 0.4f;

    [Header("Drop")]
    [SerializeField] protected GameObject dropPrefab; // geo / item drop

    // ─────────────────────────────────────────────────────────────
    //  ESTADO FSM
    // ─────────────────────────────────────────────────────────────

    protected enum EnemyState { Patrol, Chase, Attack, Stunned, Dead }
    protected EnemyState currentState = EnemyState.Patrol;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE / PROTECTED
    // ─────────────────────────────────────────────────────────────

    protected Rigidbody2D rb;
    protected Animator anim;
    protected SpriteRenderer sr;
    protected EnemyHealth health;

    protected Transform player;
    protected Vector2 patrolOrigin;
    protected bool movingRight = true;

    protected float stunTimer;
    protected float attackCooldown;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        rb     = GetComponent<Rigidbody2D>();
        anim   = GetComponent<Animator>();
        sr     = GetComponentInChildren<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();

        patrolOrigin = transform.position;

        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    protected virtual void Update()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:  UpdatePatrol();  break;
            case EnemyState.Chase:   UpdateChase();   break;
            case EnemyState.Attack:  UpdateAttack();  break;
            case EnemyState.Stunned: UpdateStunned(); break;
        }

        attackCooldown -= Time.deltaTime;
    }

    // ─────────────────────────────────────────────────────────────
    //  PATROL
    // ─────────────────────────────────────────────────────────────

    protected virtual void UpdatePatrol()
    {
        if (CanSeePlayer())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        float dir = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * patrolSpeed, rb.linearVelocity.y);

        // Flip sprite
        if (movingRight && sr != null) sr.flipX = false;
        else if (!movingRight && sr != null) sr.flipX = true;

        // Girar en los extremos
        float distFromOrigin = transform.position.x - patrolOrigin.x;
        if (movingRight  && distFromOrigin > patrolDistance)  movingRight = false;
        if (!movingRight && distFromOrigin < -patrolDistance) movingRight = true;

        anim?.SetFloat("Speed", patrolSpeed);
    }

    // ─────────────────────────────────────────────────────────────
    //  CHASE
    // ─────────────────────────────────────────────────────────────

    protected virtual void UpdateChase()
    {
        if (player == null) { ChangeState(EnemyState.Patrol); return; }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > detectionRange * 1.5f)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        if (dist <= attackRange && attackCooldown <= 0f)
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        // Moverse hacia el jugador
        float dir = (player.position.x - transform.position.x) > 0f ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);
        if (sr != null) sr.flipX = dir < 0f;

        anim?.SetFloat("Speed", chaseSpeed);
    }

    // ─────────────────────────────────────────────────────────────
    //  ATTACK — Override en subclases para patrones específicos
    // ─────────────────────────────────────────────────────────────

    protected virtual void UpdateAttack()
    {
        rb.linearVelocity = Vector2.zero;
        anim?.SetTrigger("Attack");
        attackCooldown = 1.5f;
        ChangeState(EnemyState.Chase);
    }

    // ─────────────────────────────────────────────────────────────
    //  STUN
    // ─────────────────────────────────────────────────────────────

    protected virtual void UpdateStunned()
    {
        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f)
            ChangeState(EnemyState.Chase);
    }

    public virtual void Stun()
    {
        stunTimer = stunDuration;
        ChangeState(EnemyState.Stunned);
        rb.linearVelocity = Vector2.zero;
        anim?.SetTrigger("Stun");
    }

    // ─────────────────────────────────────────────────────────────
    //  MUERTE
    // ─────────────────────────────────────────────────────────────

    public virtual void Die()
    {
        if (currentState == EnemyState.Dead) return;
        ChangeState(EnemyState.Dead);

        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        anim?.SetTrigger("Die");

        // Dar alma al jugador
        if (player != null && player.TryGetComponent<PlayerHealth>(out var ph))
            ph.GainSoul(soulsOnDeath);

        // Drop de item/geo
        if (dropPrefab != null)
            Instantiate(dropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        Destroy(gameObject, 0.8f);
    }

    // ─────────────────────────────────────────────────────────────
    //  CONTACTO (daño al tocar al jugador)
    // ─────────────────────────────────────────────────────────────

    protected virtual void OnCollisionEnter2D(Collision2D col)
    {
        if (currentState == EnemyState.Dead) return;

        if (col.gameObject.TryGetComponent<PlayerHealth>(out var ph))
            ph.TakeDamage(contactDamage, transform.position);
    }

    // ─────────────────────────────────────────────────────────────
    //  DETECCIÓN DE LÍNEA DE VISIÓN
    // ─────────────────────────────────────────────────────────────

    protected bool CanSeePlayer()
    {
        if (player == null) return false;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRange) return false;

        // Raycast para línea de visión
        Vector2 dir = (player.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, detectionRange);
        if (hit.collider != null && hit.collider.CompareTag("Player"))
            return true;

        return false;
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────

    protected void ChangeState(EnemyState newState)
    {
        currentState = newState;
        anim?.SetInteger("State", (int)newState);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  EnemyHealth — componente separado para que PlayerCombat pueda referenciarlo
// ─────────────────────────────────────────────────────────────────────────────
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 30;
    private int currentHealth;
    private EnemyBase enemyBase;

    private void Awake()
    {
        currentHealth = maxHealth;
        enemyBase = GetComponent<EnemyBase>();
    }

    public void TakeDamage(int amount, Vector3 source)
    {
        currentHealth -= amount;
        GetComponent<Animator>()?.SetTrigger("Hit");

        // Knockback leve al ser golpeado
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            Vector2 dir = ((Vector2)(transform.position - source)).normalized;
            rb.AddForce(dir * 3f, ForceMode2D.Impulse);
        }

        // Dar alma al jugador al golpear
        if (GameObject.FindWithTag("Player").TryGetComponent<PlayerHealth>(out var ph))
            ph.GainSoul();

        if (currentHealth <= 0)
            enemyBase?.Die();
        else
            enemyBase?.Stun();
    }

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
}