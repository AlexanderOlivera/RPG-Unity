using UnityEngine;

/// <summary>
/// Da vida a los aldeanos: deambulan alrededor de su casa con un ciclo de
/// caminata real y huyen despavoridos cuando un enemigo (incluidas las esporas
/// que invoca el Guardián) se acerca.
/// Conviven con <see cref="NpcInteractable"/>, así que se puede seguir hablando
/// y comerciando con ellos en cualquier momento.
/// </summary>
public class NpcWanderer : MonoBehaviour
{
    [Header("Paseo")]
    public float wanderRadius = 3.5f;
    public float walkSpeed = 2.2f;
    public float idleTime = 0.8f;

    [Header("Huida")]
    public float fleeRadius = 5f;
    public float fleeSpeed = 4.2f;
    public float fleeLifetime = 2.2f;

    private Vector2 home;
    private Vector2 target;
    private Vector2 lastMove = Vector2.down;
    private bool hasTarget;
    private float idleUntil;
    private float fleeUntil;
    private float nextScan;
    private Transform threat;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private NpcAnimator anim;
    private NpcInteractable interactable;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<NpcAnimator>();
        interactable = GetComponent<NpcInteractable>();

        // El balanceo vertical de NpcInteractable competía con el paseo: lo apaga.
        if (interactable != null) interactable.bob = false;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        home = transform.position;
        nextScan = Time.time + Random.Range(0f, 0.5f);
        idleUntil = Time.time + Random.Range(0.2f, 0.9f);
    }

    void Update()
    {
        if (GameManager.IsPaused || UIRoot.DialogueOpen) return;

        if (Time.time >= nextScan)
        {
            nextScan = Time.time + 0.5f;
            threat = FindThreat();
            if (threat != null) fleeUntil = Time.time + fleeLifetime;
        }

        Vector2 pos = transform.position;
        float speed = walkSpeed;

        if (threat != null)
        {
            Vector2 away = pos - (Vector2)threat.position;
            if (away.sqrMagnitude < 0.0001f) away = Vector2.up;
            target = ClampToHome(pos + away.normalized * 2.6f, wanderRadius * 2.4f);
            hasTarget = true;
            speed = fleeSpeed;
        }
        else if (Time.time < fleeUntil)
        {
            speed = fleeSpeed;
        }
        else if (!hasTarget || Vector2.Distance(pos, target) < 0.18f)
        {
            hasTarget = false;
            if (Time.time >= idleUntil) PickTarget();
        }

        if (!hasTarget)
        {
            Rest();
            return;
        }

        Vector2 delta = target - pos;
        if (delta.sqrMagnitude < 0.0004f)
        {
            Rest();
            return;
        }

        Vector2 step = delta.normalized * speed * Time.deltaTime;
        if (step.sqrMagnitude > delta.sqrMagnitude) step = delta;
        Vector2 next = pos + step;

        if (rb != null) rb.MovePosition(next);
        else transform.position = next;

        lastMove = step;
        if (anim != null) anim.Animate(step, true);
        else Face(step.x);
    }

    /// <summary>Quieto: reproduce el primer fotograma para no quedar en pose rara.</summary>
    void Rest()
    {
        if (anim != null) anim.Animate(lastMove, false);
        else Face(lastMove.x);
    }

    void PickTarget()
    {
        float angle = Random.value * Mathf.PI * 2f;
        float radius = Random.Range(0.8f, wanderRadius);
        target = ClampToHome(home + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, wanderRadius);
        hasTarget = true;
        idleUntil = Time.time + idleTime + Random.Range(0f, 0.8f);
    }

    Vector2 ClampToHome(Vector2 point, float maxRadius)
    {
        Vector2 offset = point - home;
        if (offset.magnitude > maxRadius) offset = offset.normalized * maxRadius;
        return home + offset;
    }

    Transform FindThreat()
    {
        EnemyAI[] enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        Transform best = null;
        float bestDistance = fleeRadius;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyAI enemy = enemies[i];
            if (enemy == null) continue;

            Health h = enemy.GetComponent<Health>();
            if (h != null && !h.IsAlive) continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = enemy.transform;
            }
        }
        return best;
    }

    void Face(float directionX)
    {
        if (sr == null || Mathf.Abs(directionX) < 0.01f) return;
        sr.flipX = directionX < 0f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.7f);
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)home : transform.position, wanderRadius);
        Gizmos.color = new Color(1f, 0.55f, 0.35f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, fleeRadius);
    }
}
