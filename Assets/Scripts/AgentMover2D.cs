using UnityEngine;

/// <summary>
/// Motor de desplazamiento para agentes con Rigidbody2D. Avanza en línea recta
/// hacia el destino, se separa de otros agentes y rodea obstáculos con un
/// barrido de colisión delante. No usa rejilla ni cálculo de rutas: sólo
/// dirección, separación y evasión.
/// Todo el movimiento ocurre en FixedUpdate (MovePosition) para que las
/// colisiones se resuelvan sin vibraciones.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AgentMover2D : MonoBehaviour
{
    public float repathInterval = 0.35f;
    public float waypointTolerance = 0.22f;
    public float separationRadius = 0.55f;
    public float separationStrength = 0.6f;

    [Header("Evasión de obstáculos")]
    [Tooltip("Distancia del barrido que busca paredes delante del agente.")]
    public float obstacleProbe = 0.9f;

    [Tooltip("Cuánto pesa el desvío lateral al rodear un obstáculo.")]
    public float avoidStrength = 1.2f;

    private Rigidbody2D rb;
    private Collider2D body;
    private LayerMask obstacleMask;
    private Vector2 goal;
    private Vector2 avoid;
    private float nextProbe;
    private bool hasGoal;

    public float CurrentSpeed { get; private set; }
    public bool HasGoal { get { return hasGoal; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        LayerMask walls = LayerMask.GetMask("Walls");
        obstacleMask = (walls.value != 0 ? walls.value : 0) | (1 << 0);
    }

    /// <summary>Fija el destino y la velocidad. Repite la llamada en cada frame.</summary>
    public void MoveTo(Vector2 target, float speed)
    {
        goal = target;
        CurrentSpeed = Mathf.Max(0.1f, speed);
        hasGoal = true;

        if (Time.time < nextProbe) return;
        nextProbe = Time.time + Mathf.Max(0.1f, repathInterval);

        Vector2 dir = target - (rb != null ? rb.position : (Vector2)transform.position);
        avoid = DetectObstacle(dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero);
    }

    public void Stop()
    {
        hasGoal = false;
        CurrentSpeed = 0f;
        avoid = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (!hasGoal || rb == null) return;

        Vector2 pos = rb.position;
        Vector2 toGoal = goal - pos;
        if (toGoal.sqrMagnitude <= waypointTolerance * waypointTolerance) { Stop(); return; }

        Vector2 dir = toGoal.normalized + avoid * avoidStrength + Separation() * separationStrength;
        if (dir.sqrMagnitude < 0.0001f) return;

        rb.MovePosition(pos + dir.normalized * CurrentSpeed * Time.fixedDeltaTime);
    }

    /// <summary>Barrido frontal: si hay pared, devuelve el vector lateral libre.</summary>
    Vector2 DetectObstacle(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f || rb == null) return Vector2.zero;

        Vector2 origin = rb.position + dir * BodyRadius();
        if (!IsObstacle(origin, dir, obstacleProbe)) return Vector2.zero;

        Vector2 side = new Vector2(-dir.y, dir.x);
        Vector2 left = (dir - side * 0.75f).normalized;
        Vector2 right = (dir + side * 0.75f).normalized;

        bool leftBlocked = IsObstacle(origin, left, obstacleProbe);
        bool rightBlocked = IsObstacle(origin, right, obstacleProbe);

        if (!leftBlocked) return -side;
        if (!rightBlocked) return side;
        return side;
    }

    /// <summary>Ignora personajes, disparadores y el propio cuerpo: sólo cuenta lo sólido.</summary>
    bool IsObstacle(Vector2 origin, Vector2 dir, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, distance, obstacleMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i].collider;
            if (hit == null || hit.isTrigger) continue;
            if (hit.attachedRigidbody == rb || hit.transform == transform) continue;
            if (hit.GetComponentInParent<AgentMover2D>() != null) continue;
            if (hit.GetComponentInParent<PlayerController>() != null) continue;
            if (hit.GetComponentInParent<NpcWanderer>() != null) continue;
            return true;
        }
        return false;
    }

    float BodyRadius()
    {
        if (body == null) body = GetComponent<Collider2D>();
        if (body == null) return 0.25f;
        return Mathf.Max(0.05f, Mathf.Max(body.bounds.extents.x, body.bounds.extents.y) * 0.75f);
    }

    Vector2 Separation()
    {
        Vector2 push = Vector2.zero;
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, separationRadius);
        for (int i = 0; i < cols.Length; i++)
        {
            AgentMover2D other = cols[i].GetComponentInParent<AgentMover2D>();
            if (other == null || other == this) continue;

            Vector2 diff = (Vector2)transform.position - (Vector2)other.transform.position;
            if (diff.sqrMagnitude < 0.0001f) continue;
            push += diff.normalized * (1f - Mathf.Clamp01(diff.magnitude / separationRadius));
        }
        return push;
    }
}
