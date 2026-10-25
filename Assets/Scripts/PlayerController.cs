using UnityEngine;

/// <summary>
/// Jugador: movimiento 4 direcciones, ataque cuerpo a cuerpo (Espacio) y
/// ataque a distancia (F / clic derecho). Bloquea input en pausa y diálogo.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;

    [Header("Cuerpo a cuerpo")]
    public float attackRange = 1.2f;
    public int attackDamage = 10;
    public LayerMask enemyLayers;
    public float meleeCooldown = 0.45f;

    [Header("A distancia")]
    public GameObject projectilePrefab;
    public float rangedCooldown = 0.75f;
    public int rangedDamage = 8;
    public float projectileSpeed = 9f;
    public Transform projectileOrigin;

    [Header("Esquiva (Shift)")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.16f;
    public float dashCooldown = 0.75f;
    public float dashInvulnerability = 0.28f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 movement;
    private Vector2 facing = Vector2.down;
    private float nextMelee;
    private float nextRanged;
    private float nextDash;
    private float dashUntil;
    private Vector2 dashDirection = Vector2.down;

    public Vector2 Facing { get { return facing; } }
    public bool IsDashing { get { return Time.time < dashUntil; } }
    public bool InputEnabled { get { return !GameManager.IsPaused && !UIRoot.DialogueOpen; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (projectileOrigin == null) projectileOrigin = transform;
    }

    void Start()
    {
        if (enemyLayers.value == 0) enemyLayers = LayerMask.GetMask("Enemies");
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void Update()
    {
        if (!InputEnabled)
        {
            movement = Vector2.zero;
            UpdateAnimator();
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        if (movement.sqrMagnitude > 0.01f) facing = movement.normalized;

        UpdateAnimator();

        if (Input.GetKeyDown(KeyCode.Space)) MeleeAttack();
        if (Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1)) RangedAttack();
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) TryDash();
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", movement.sqrMagnitude);
        if (movement.sqrMagnitude > 0.01f)
        {
            animator.SetFloat("X", movement.x);
            animator.SetFloat("Y", movement.y);
        }
        if (spriteRenderer != null && Mathf.Abs(facing.x) > 0.01f) spriteRenderer.flipX = facing.x < 0f;
    }

    void FixedUpdate()
    {
        if (rb == null || !InputEnabled) return;

        if (IsDashing)
        {
            rb.MovePosition(rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime);
            return;
        }

        if (movement.sqrMagnitude < 0.0001f) return;
        rb.MovePosition(rb.position + movement.normalized * speed * PlayerBuffs.SpeedMultiplier * Time.fixedDeltaTime);
    }

    /// <summary>Esquiva con invulnerabilidad breve: la herramienta clave contra las esporas.</summary>
    public bool TryDash()
    {
        if (Time.time < nextDash || IsDashing) return false;

        nextDash = Time.time + dashCooldown;
        dashUntil = Time.time + dashDuration;
        dashDirection = movement.sqrMagnitude > 0.01f ? movement.normalized : facing;

        Health health = GetComponent<Health>();
        if (health != null) health.GrantInvulnerability(dashInvulnerability);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.Slash);
        return true;
    }

    /// <summary>Ataque cuerpo a cuerpo con cono frontal y cooldown real.</summary>
    public int MeleeAttack()
    {
        if (Time.time < nextMelee) return 0;
        nextMelee = Time.time + meleeCooldown;

        if (animator != null)
        {
            animator.SetFloat("X", facing.x);
            animator.SetFloat("Y", facing.y);
            animator.SetTrigger("Attack");
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.Slash);

        int hits = 0;
        int damage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * PlayerBuffs.DamageMultiplier));
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayers);
        for (int i = 0; i < cols.Length; i++)
        {
            Health h = cols[i].GetComponentInParent<Health>();
            if (h == null || h.isPlayer) continue;

            Vector2 dir = ((Vector2)cols[i].transform.position - (Vector2)transform.position);
            if (dir.sqrMagnitude > 0.0001f && Vector2.Dot(dir.normalized, facing) < 0.15f) continue;

            if (h.TakeDamage(damage)) hits++;
        }
        return hits;
    }

    /// <summary>Ataque a distancia: instancia el proyectil y lo lanza hacia la dirección actual.</summary>
    public int RangedAttack()
    {
        if (Time.time < nextRanged) return 0;
        nextRanged = Time.time + rangedCooldown;

        if (animator != null)
        {
            animator.SetFloat("X", facing.x);
            animator.SetFloat("Y", facing.y);
            animator.SetTrigger("AttackRanged");
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.Slash);

        if (projectilePrefab == null) return 0;

        Vector3 spawnPos = projectileOrigin.position + (Vector3)(facing * 0.45f);
        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        PlayerProjectile proj = go.GetComponent<PlayerProjectile>();
        int damage = Mathf.Max(1, Mathf.RoundToInt(rangedDamage * PlayerBuffs.DamageMultiplier));
        if (proj != null) proj.Launch(facing, projectileSpeed, damage);
        return 1;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
