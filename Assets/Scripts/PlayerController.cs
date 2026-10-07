using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // --- Variables de Combate ---
    private Vector2 lastMoveDirection;
    public float attackRange = 0.6f;
    public int attackDamage = 10;
    public LayerMask enemyLayers;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        lastMoveDirection = new Vector2(0, -1); // Por defecto mira hacia abajo al iniciar
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Control del Animator y dirección
        if (movement != Vector2.zero)
        {
            lastMoveDirection = movement.normalized; // Guardamos hacia dónde miró por última vez
            animator.SetFloat("X", movement.x);
            animator.SetFloat("Y", movement.y);
        }

        // Voltear el sprite
        if (movement.x < 0)
        {
            spriteRenderer.flipX = true; // Mira a la izquierda
        }
        else if (movement.x > 0)
        {
            spriteRenderer.flipX = false; // Mira a la derecha
        }

        // Detectar tecla de ataque (Espacio)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Attack();
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement.normalized * speed * Time.fixedDeltaTime);
    }

    void Attack()
    {
        // 1. Calcular el centro del golpe frente al jugador
        Vector2 attackPosition = (Vector2)transform.position + (lastMoveDirection * 0.5f);

        // 2. Detectar todos los enemigos dentro del círculo
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPosition, attackRange, enemyLayers);

        // 3. Aplicar daño a los enemigos detectados
        foreach (Collider2D enemy in hitEnemies)
        {
            // Buscamos el componente Health en el objeto que golpeamos
            Health enemyHealth = enemy.GetComponent<Health>();

            // Si el objeto realmente tiene un sistema de salud, le hacemos daño
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
        }
    }

    // Esta función de Unity dibuja el hitbox rojo en la pestaña Scene para que puedas ajustarlo
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 attackPosition = (Vector2)transform.position + (lastMoveDirection * 0.5f);
        Gizmos.DrawWireSphere(attackPosition, attackRange);
    }
}