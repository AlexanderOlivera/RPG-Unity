using UnityEngine;

/// <summary>
/// Cerebro del enemigo: aguarda en su zona, detecta al jugador por radio de
/// agresión, lo persigue en línea recta (el motor AgentMover2D rodea las
/// paredes), ataca al alcance y vuelve a su puesto si el jugador se aleja.
/// No depende de ninguna rejilla de navegación.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("Configuración del Enemigo")]
    public Transform player;
    public float speed = 2.5f;
    public float attackRange = 1.1f;
    public int attackDamage = 5;
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
    private float nextPlayerSearch;
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

        FindPlayer();
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

    void FindPlayer()
    {
        if (player != null) return;

        PlayerController found = Object.FindFirstObjectByType<PlayerController>();
        if (found != null)
        {
            player = found.transform;
            return;
        }

        GameObject tagged = null;
        try { tagged = GameObject.FindGameObjectWithTag("Player"); }
        catch (UnityException) { tagged = null; }
        if (tagged != null) player = tagged.transform;
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
        if (GameManager.IsPaused || UIRoot.DialogueOpen)
        {
            if (mover != null) mover.Stop();
            UpdateAnimator(false);
            return;
        }

        if (health == null || !health.IsAlive) return;

        // Búsqueda perezosa del jugador: si aparece en la escena más tarde
        // (o se reconstruye al reiniciar), el enemigo lo vuelve a encontrar.
        if (player == null)
        {
            if (Time.time < nextPlayerSearch) return;
            nextPlayerSearch = Time.time + 0.5f;
            FindPlayer();
            if (player == null) return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float distanceToHome = Vector2.Distance(transform.position, home);

        if (!aggro && distanceToPlayer <= aggroRange) aggro = true;
        if (aggro && (distanceToHome > leashRange || distanceToPlayer > leashRange)) aggro = false;

        if (aggro)
        {
            if (distanceToPlayer > attackRange)
            {
                MoveToward(player.position, speed);
                UpdateAnimator(true);
            }
            else
            {
                MoveToward(Vector2.zero, 0f);
                UpdateAnimator(false);
                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackCooldown;
                    AttackPlayer();
                }
            }
            return;
        }

        if (distanceToHome > homeRadius)
        {
            MoveToward(home, speed * 0.7f);
            UpdateAnimator(true);
        }
        else
        {
            MoveToward(Vector2.zero, 0f);
            UpdateAnimator(false);
        }
    }

    /// <summary>Delega el desplazamiento en AgentMover2D o empuja el cuerpo directamente.</summary>
    void MoveToward(Vector2 target, float moveSpeed)
    {
        if (moveSpeed <= 0f)
        {
            if (mover != null) mover.Stop();
            return;
        }

        if (mover != null)
        {
            mover.MoveTo(target, moveSpeed);
            return;
        }

        if (rb == null) return;
        rb.MovePosition(Vector2.MoveTowards(rb.position, target, moveSpeed * Time.fixedDeltaTime));
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
