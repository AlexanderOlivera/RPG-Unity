using UnityEngine;

/// <summary>
/// IA de enemigo: detecta al jugador por radio de agresión, lo persigue con
/// navegación A* (AgentMover2D), ataca al alcance y vuelve a su zona de
/// vigilancia si el jugador se aleja demasiado (leash).
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("Configuración del Enemigo")]
    public Transform player;
    public float speed = 2.5f;
    public float attackRange = 1.1f;
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;

    [Header("Zona de vigilancia")]
    public float aggroRange = 7f;
    public float leashRange = 10f;
    public float homeRadius = 0.6f;

    [Header("Recompensa")]
    public int scoreReward = 100;
    public bool isBoss = false;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Health health;
    private AgentMover2D mover;
    private Vector2 home;
    private Vector2 facing = Vector2.down;
    private float nextAttackTime;
    private bool aggro;
    private bool subscribed;
    private bool healthScaled;

    public bool IsAggro { get { return aggro; } }

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        mover = GetComponent<AgentMover2D>();
        home = transform.position;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        ApplyDifficultyScaling();
        Subscribe();
    }

    void OnEnable()
    {
        Subscribe();
    }

    void OnDisable()
    {
        if (subscribed && health != null) health.OnDied -= HandleDeath;
        subscribed = false;
    }

    void ApplyDifficultyScaling()
    {
        if (healthScaled || health == null || health.isPlayer) return;
        healthScaled = true;

        float mult = Difficulty.EnemyHealthMultiplier;
        if (Mathf.Approximately(mult, 1f)) return;

        health.maxHealth = Mathf.Max(1, Mathf.RoundToInt(health.maxHealth * mult));
        health.ResetHealth();
    }

    void Subscribe()
    {
        if (subscribed || health == null) return;
        health.OnDied += HandleDeath;
        subscribed = true;
    }

    void HandleDeath(Health h)
    {
        GameSession.RegisterKill();
        ScoreSystem.AwardKill(scoreReward);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.EnemyDeath);
        if (isBoss) GameManager.NotifyBossDefeated();
    }

    void Update()
    {
        if (GameManager.IsPaused || UIRoot.DialogueOpen) return;
        if (player == null || health == null || !health.IsAlive) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float distanceToHome = Vector2.Distance(transform.position, home);

        if (!aggro && distanceToPlayer <= aggroRange) aggro = true;
        if (aggro && (distanceToHome > leashRange || distanceToPlayer > leashRange)) aggro = false;

        if (aggro)
        {
            if (distanceToPlayer > attackRange)
            {
                if (mover != null) mover.MoveTo(player.position, speed);
                UpdateAnimator(true);
            }
            else
            {
                if (mover != null) mover.Stop();
                UpdateAnimator(false);
                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackCooldown;
                    AttackPlayer();
                }
            }
        }
        else
        {
            if (distanceToHome > homeRadius)
            {
                if (mover != null) mover.MoveTo(home, speed * 0.7f);
                UpdateAnimator(true);
            }
            else
            {
                if (mover != null) mover.Stop();
                UpdateAnimator(false);
            }
        }
    }

    void UpdateAnimator(bool moving)
    {
        if (animator == null) return;
        Vector2 dir = player != null ? ((Vector2)player.position - (Vector2)transform.position).normalized : facing;
        if (dir.sqrMagnitude > 0.001f)
        {
            facing = dir;
            animator.SetFloat("X", dir.x);
            animator.SetFloat("Y", dir.y);
        }
        animator.SetFloat("Speed", moving ? 1f : 0f);
        if (spriteRenderer != null && Mathf.Abs(facing.x) > 0.01f) spriteRenderer.flipX = facing.x < 0f;
    }

    void AttackPlayer()
    {
        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth == null) playerHealth = player.GetComponentInParent<Health>();
        if (playerHealth == null) return;

        int damage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * Difficulty.DamageMultiplier));
        if (playerHealth.TakeDamage(damage) && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxId.Hurt);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)home : transform.position, leashRange);
    }
}
