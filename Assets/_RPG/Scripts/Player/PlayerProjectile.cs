using UnityEngine;

/// <summary>Proyectil del ataque a distancia: daña enemigos y muere al impactar.</summary>
public class PlayerProjectile : MonoBehaviour
{
    public float lifeTime = 2f;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.right;
    private float speed = 9f;
    private int damage = 8;
    private bool launched;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector2 dir, float spd, int dmg)
    {
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        direction = dir.normalized;
        speed = Mathf.Max(0.5f, spd);
        damage = Mathf.Max(1, dmg);
        launched = true;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void FixedUpdate()
    {
        if (!launched) return;
        Vector3 next = transform.position + (Vector3)(direction * speed * Time.fixedDeltaTime);
        if (rb != null) rb.MovePosition(next);
        else transform.position = next;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!launched || other == null) return;
        if (other.GetComponentInParent<PlayerController>() != null) return;

        Health health = other.GetComponentInParent<Health>();
        if (health != null && !health.isPlayer)
        {
            health.TakeDamage(damage);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.Hurt);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger) Destroy(gameObject);
    }
}
