using UnityEngine;
using System.Collections;

/// <summary>
/// TOKYO METROIDVANIA - DroneEnemy
/// Drone de vigilancia semi-futurista. Vuela y dispara proyectiles de energía.
/// Hereda de EnemyBase.
/// </summary>
public class DroneEnemy : EnemyBase
{
    [Header("Drone - Vuelo")]
    [SerializeField] private float floatAmplitude = 0.5f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float preferredHeight = 3f; // vuela a esta altura sobre el suelo

    [Header("Drone - Disparo")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private int laserDamage = 8;
    [SerializeField] private float laserSpeed = 10f;
    [SerializeField] private int burstCount = 3;          // disparos en ráfaga
    [SerializeField] private float burstDelay = 0.15f;

    private float fireTimer;
    private float floatOffset;
    private Vector2 patrolFloatOrigin;

    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0f; // el drone no tiene gravedad
        patrolFloatOrigin = transform.position;
        floatOffset = Random.Range(0f, Mathf.PI * 2f); // offset aleatorio de fase
    }

    protected override void Update()
    {
        base.Update();
        ApplyFloatMotion();
        fireTimer -= Time.deltaTime;
    }

    // Hover sinusoidal
    private void ApplyFloatMotion()
    {
        if (currentState == EnemyState.Dead) return;

        float wave = Mathf.Sin(Time.time * floatSpeed + floatOffset) * floatAmplitude;
        Vector3 pos = transform.position;

        // Mantener altura preferida sobre el suelo
        RaycastHit2D groundHit = Physics2D.Raycast(pos, Vector2.down, 10f, LayerMask.GetMask("Ground"));
        float targetY = groundHit.collider != null
            ? groundHit.point.y + preferredHeight + wave
            : pos.y + wave * 0.1f;

        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * 3f);
        transform.position = pos;
    }

    protected override void UpdatePatrol()
    {
        if (CanSeePlayer())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Patrulla horizontal sin gravedad
        float dir = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * patrolSpeed, 0f);
        if (sr != null) sr.flipX = !movingRight;

        float distX = transform.position.x - patrolFloatOrigin.x;
        if (movingRight  && distX >  patrolDistance) movingRight = false;
        if (!movingRight && distX < -patrolDistance) movingRight = true;
    }

    protected override void UpdateChase()
    {
        if (player == null) { ChangeState(EnemyState.Patrol); return; }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > detectionRange * 1.5f) { ChangeState(EnemyState.Patrol); return; }

        // Mantenerse a distancia de ataque óptima
        if (dist > attackRange)
        {
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * chaseSpeed;
            if (sr != null) sr.flipX = dir.x < 0f;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Disparar en chase si está en rango
        if (dist <= detectionRange && fireTimer <= 0f)
        {
            StartCoroutine(FireBurst());
            fireTimer = fireRate;
        }
    }

    protected override void UpdateAttack()
    {
        // El drone dispara desde chase, no necesita estado de ataque separado
        ChangeState(EnemyState.Chase);
    }

    private IEnumerator FireBurst()
    {
        if (player == null || laserPrefab == null) yield break;

        anim?.SetTrigger("Attack");

        for (int i = 0; i < burstCount; i++)
        {
            SpawnLaser();
            yield return new WaitForSeconds(burstDelay);
        }
    }

    private void SpawnLaser()
    {
        if (firePoint == null || player == null) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)firePoint.position).normalized;

        GameObject laser = Instantiate(laserPrefab, firePoint.position, Quaternion.identity);
        if (laser.TryGetComponent<Rigidbody2D>(out var lrb))
            lrb.linearVelocity = dir * laserSpeed;

        if (laser.TryGetComponent<ProjectileBase>(out var proj))
            proj.SetDamage(laserDamage);

        // Rotar el sprite del láser hacia el jugador
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        laser.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(laser, 3f);
    }

    // Los drones son más difíciles de hacer pogo sobre ellos
    public override void Stun()
    {
        base.Stun();
        // Efecto visual: parpadeo de LEDs neon
        StartCoroutine(FlickerOnStun());
    }

    private IEnumerator FlickerOnStun()
    {
        for (int i = 0; i < 6; i++)
        {
            if (sr != null) sr.color = Color.cyan;
            yield return new WaitForSeconds(0.05f);
            if (sr != null) sr.color = Color.white;
            yield return new WaitForSeconds(0.05f);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  ProjectileBase — componente del proyectil del drone
// ─────────────────────────────────────────────────────────────────────────────
public class ProjectileBase : MonoBehaviour
{
    private int damage = 8;

    public void SetDamage(int d) => damage = d;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            if (col.TryGetComponent<PlayerHealth>(out var ph))
                ph.TakeDamage(damage, transform.position);
            Destroy(gameObject);
        }
        else if (col.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}