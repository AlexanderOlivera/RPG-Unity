using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float speed = 2f;
    public float attackRange = 1f; // Distancia a la que se detiene para atacar
    public int attackDamage = 5;
    public float attackCooldown = 1.5f; // Segundos entre cada ataque

    private float lastAttackTime;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Busca automáticamente al jugador si no lo asignaste en el Inspector
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        // Voltear el sprite para mirar siempre al jugador
        if (player.position.x < transform.position.x) spriteRenderer.flipX = true;
        else if (player.position.x > transform.position.x) spriteRenderer.flipX = false;

        // Lógica de ataque
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackPlayer();
                lastAttackTime = Time.time;
            }
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Moverse solo si el jugador está fuera del rango de ataque
        if (distanceToPlayer > attackRange)
        {
            Vector2 targetPosition = Vector2.MoveTowards(rb.position, player.position, speed * Time.fixedDeltaTime);
            rb.MovePosition(targetPosition);
        }
    }

    void AttackPlayer()
    {
        Health playerHealth = player.GetComponent<Health>();

        if (playerHealth != null)
        {
            Debug.Log("¡El " + gameObject.name + " ataca al jugador por " + attackDamage + " de daño!");
            playerHealth.TakeDamage(attackDamage);
        }
    }

    // Dibuja un círculo amarillo en la escena para ver el rango de ataque del enemigo
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}