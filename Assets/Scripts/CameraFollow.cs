using UnityEngine;

/// <summary>
/// Seguimiento de cámara independiente del framerate (SmoothDamp + deltaTime)
/// con límites opcionales tomados de un Collider2D (LimitesCamara).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.12f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    public Collider2D bounds;
    public bool useBounds = true;
    public bool snapOnStart = true;

    private Camera cam;
    private Vector3 velocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target == null) target = FindPlayer();
        if (snapOnStart && target != null) transform.position = ClampToBounds(DesiredPosition());
    }

    void LateUpdate()
    {
        if (target == null)
        {
            target = FindPlayer();
            if (target == null) return;
        }

        Vector3 goal = ClampToBounds(DesiredPosition());
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, Mathf.Max(0.0001f, smoothTime), Mathf.Infinity, dt);
    }

    Vector3 DesiredPosition()
    {
        Vector3 p = target.position + offset;
        p.z = offset.z;
        return p;
    }

    public void SetBounds(Collider2D newBounds, bool snap = true)
    {
        bounds = newBounds;
        if (snap)
        {
            if (target == null) target = FindPlayer();
            if (target != null) transform.position = ClampToBounds(DesiredPosition());
        }
    }

    Vector3 ClampToBounds(Vector3 position)
    {
        if (!useBounds || bounds == null || cam == null || !cam.orthographic) return position;

        Bounds b = bounds.bounds;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        if (b.size.x > halfWidth * 2f)
            position.x = Mathf.Clamp(position.x, b.min.x + halfWidth, b.max.x - halfWidth);
        else
            position.x = b.center.x;

        if (b.size.y > halfHeight * 2f)
            position.y = Mathf.Clamp(position.y, b.min.y + halfHeight, b.max.y - halfHeight);
        else
            position.y = b.center.y;

        position.z = offset.z;
        return position;
    }

    static Transform FindPlayer()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        return go != null ? go.transform : null;
    }
}
