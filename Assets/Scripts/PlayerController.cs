using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float attackRange = 0.6f;
    public int attackDamage = 10;
    public LayerMask enemyLayers;

    [Header("Disparo con Arco")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    private Vector2 lastMoveDirection = Vector2.down; // Para saber a dónde mirar al estar quieto

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // BLOQUEO DE PAUSA Y GAME OVER: 
        // Si el tiempo está congelado, ignora todo este código y no permite moverse ni disparar.
        if (Time.timeScale == 0f) return;

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        animator.SetFloat("Speed", movement.sqrMagnitude);

        if (movement.x != 0 || movement.y != 0)
        {
            animator.SetFloat("X", movement.x);
            animator.SetFloat("Y", movement.y);

            // Guardar la dirección para la flecha
            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
                lastMoveDirection = new Vector2(movement.x, 0).normalized;
            else
                lastMoveDirection = new Vector2(0, movement.y).normalized;
        }

        if (movement.x < 0)
            spriteRenderer.flipX = true;
        else if (movement.x > 0)
            spriteRenderer.flipX = false;

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
        animator.SetTrigger("Attack");

        // Instanciar y disparar la flecha
        if (arrowPrefab != null && firePoint != null)
        {
            GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
            arrow.GetComponent<Arrow>().Setup(lastMoveDirection);
        }

        /* LÓGICA CUERPO A CUERPO ORIGINAL (Comentada para evitar daño doble)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null)
            {
                Debug.Log("Golpeaste a " + enemy.name);
                enemyHealth.TakeDamage(attackDamage);
            }
        }
        */
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}