using UnityEngine;
using System.Collections;

/// <summary>
/// TOKYO METROIDVANIA - PlayerCombat (Unity 6 - New Input System)
/// X = Atacar en 4 direcciones
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private int pogoAttackDamage = 12;
    [SerializeField] private int upAttackDamage = 12;

    [Header("Hitboxes")]
    [SerializeField] private Transform attackPointRight;
    [SerializeField] private Transform attackPointLeft;
    [SerializeField] private Transform attackPointUp;
    [SerializeField] private Transform attackPointDown;
    [SerializeField] private float attackRadius = 0.6f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Timing")]
    [SerializeField] private float attackDuration = 0.25f;
    [SerializeField] private float attackCooldown = 0.35f;

    [Header("Pogo")]
    [SerializeField] private float pogoBounceForce = 14f;

    private Rigidbody2D rb;
    private Animator anim;
    private PlayerController playerController;
    private PlayerHealth playerHealth;

    private float cooldownTimer;
    private bool isAttacking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;
    }

    /// <summary>Llamado por PlayerController cuando se presiona X</summary>
    public void TriggerAttack(Vector2 input)
    {
        if (cooldownTimer > 0f || isAttacking) return;

        AttackDirection dir = DetermineDirection(input);
        StartCoroutine(PerformAttack(dir));
    }

    private enum AttackDirection { Right, Left, Up, Down }

    private AttackDirection DetermineDirection(Vector2 input)
    {
        if (input.y > 0.5f) return AttackDirection.Up;
        if (input.y < -0.5f && !playerController.IsGrounded) return AttackDirection.Down;
        return playerController.FacingRight ? AttackDirection.Right : AttackDirection.Left;
    }

    private IEnumerator PerformAttack(AttackDirection dir)
    {
        isAttacking = true;
        cooldownTimer = attackCooldown;
        playerController.SetAttacking(true);

        string animTrigger = dir switch
        {
            AttackDirection.Up   => "AttackUp",
            AttackDirection.Down => "AttackDown",
            _                    => "Attack"
        };
        anim.SetTrigger(animTrigger);

        yield return new WaitForSeconds(attackDuration * 0.5f);

        Transform hitPoint = dir switch
        {
            AttackDirection.Up    => attackPointUp,
            AttackDirection.Down  => attackPointDown,
            AttackDirection.Right => attackPointRight,
            AttackDirection.Left  => attackPointLeft,
            _                     => attackPointRight
        };

        int damage = dir switch
        {
            AttackDirection.Up   => upAttackDamage,
            AttackDirection.Down => pogoAttackDamage,
            _                    => attackDamage
        };

        if (hitPoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(hitPoint.position, attackRadius, enemyLayer);
            bool hitSomething = false;

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<EnemyHealth>(out var enemy))
                {
                    enemy.TakeDamage(damage, transform.position);
                    hitSomething = true;
                }
            }

            // Pogo bounce
            if (dir == AttackDirection.Down && hitSomething)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * pogoBounceForce, ForceMode2D.Impulse);
            }
        }

        yield return new WaitForSeconds(attackDuration * 0.5f);

        playerController.SetAttacking(false);
        isAttacking = false;
    }

    public bool IsAttacking => isAttacking;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.7f);
        if (attackPointRight != null) Gizmos.DrawWireSphere(attackPointRight.position, attackRadius);
        if (attackPointLeft  != null) Gizmos.DrawWireSphere(attackPointLeft.position,  attackRadius);
        if (attackPointUp    != null) Gizmos.DrawWireSphere(attackPointUp.position,    attackRadius);
        if (attackPointDown  != null) Gizmos.DrawWireSphere(attackPointDown.position,  attackRadius);
    }
}