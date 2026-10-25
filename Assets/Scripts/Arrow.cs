using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 2f;
    public int damage = 10;
    private Rigidbody2D rb;

    public void Setup(Vector2 moveDirection)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = moveDirection.normalized * speed;

        // Calculamos el ángulo y le restamos 90 grados porque el sprite original mira hacia arriba
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // BLOQUEO DE PAUSA: Evita que la flecha procese impactos si el tiempo está congelado
        if (Time.timeScale == 0f) return;

        if (hitInfo.CompareTag("Player"))
        {
            return;
        }

        Health enemyHealth = hitInfo.GetComponent<Health>();

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
            Debug.Log("La flecha impactó a " + hitInfo.name);
            Destroy(gameObject);
        }
        else if (hitInfo.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
    }
}