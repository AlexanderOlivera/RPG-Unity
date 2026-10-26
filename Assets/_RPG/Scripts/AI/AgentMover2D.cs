using UnityEngine;

/// <summary>
/// Mueve un Rigidbody2D siguiendo la ruta calculada por NavGridBuilder.
/// Todo el movimiento ocurre en FixedUpdate (MovePosition) para evitar
/// vibraciones y para que las colisiones se resuelvan correctamente.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AgentMover2D : MonoBehaviour
{
    public float repathInterval = 0.35f;
    public float waypointTolerance = 0.22f;
    public float separationRadius = 0.55f;
    public float separationStrength = 0.6f;

    private Rigidbody2D rb;
    private System.Collections.Generic.List<Vector2> path = new System.Collections.Generic.List<Vector2>();
    private int index;
    private Vector2 goal;
    private float nextRepath;
    private bool hasGoal;

    public float CurrentSpeed { get; private set; }
    public bool HasPath { get { return path.Count > 0; } }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    public void MoveTo(Vector2 target, float speed)
    {
        if (rb == null) return;
        goal = target;
        CurrentSpeed = speed;
        hasGoal = true;

        bool needRepath = path.Count == 0 || Time.time >= nextRepath;
        if (!needRepath) return;

        nextRepath = Time.time + Mathf.Max(0.1f, repathInterval);

        NavGridBuilder nav = NavGridBuilder.Instance;
        System.Collections.Generic.List<Vector2> route = nav != null ? nav.FindPath(rb.position, target) : null;
        if (route != null && route.Count > 0)
        {
            path = route;
            index = 0;
        }
        else
        {
            path.Clear();
            path.Add(target);
            index = 0;
        }
    }

    public void Stop()
    {
        hasGoal = false;
        CurrentSpeed = 0f;
        path.Clear();
        index = 0;
    }

    void FixedUpdate()
    {
        if (!hasGoal || rb == null || path.Count == 0) return;

        Vector2 pos = rb.position;
        while (index < path.Count && Vector2.Distance(pos, path[index]) <= waypointTolerance) index++;
        if (index >= path.Count) { Stop(); return; }

        Vector2 dir = path[index] - pos;
        if (dir.sqrMagnitude < 0.0001f) { index++; return; }
        dir.Normalize();
        dir += Separation() * separationStrength;
        if (dir.sqrMagnitude < 0.0001f) return;

        Vector2 next = pos + dir.normalized * Mathf.Max(0.1f, CurrentSpeed) * Time.fixedDeltaTime;
        rb.MovePosition(next);
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
