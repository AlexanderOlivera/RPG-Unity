using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float speed = 2.5f;
    public float attackRange = 1.1f;
    public int attackDamage = 5;
    public float attackCooldown = 1.5f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Nueva variable secreta para llevar la cuenta regresiva del ataque
    private float nextAttackTime = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange)
        {
            // 1. Perseguir al jugador
            transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);

            Vector2 direction = (player.position - transform.position).normalized;

            animator.SetFloat("X", direction.x);
            animator.SetFloat("Y", direction.y);
            animator.SetFloat("Speed", speed);

            if (direction.x < 0)
                spriteRenderer.flipX = true;
            else if (direction.x > 0)
                spriteRenderer.flipX = false;
        }
        else
        {
            // 2. El enemigo está lo suficientemente cerca para atacar, así que se detiene
            animator.SetFloat("Speed", 0);

            // 3. Revisar si ya pasó el tiempo de espera (Cooldown) para poder atacar
            if (Time.time >= nextAttackTime)
            {
                AtacarJugador();

                // Reiniciar el cronómetro para el siguiente golpe
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    void AtacarJugador()
    {
        // (Opcional) Si luego le haces animación de ataque al topo, aquí pondrías: animator.SetTrigger("Attack");

        // Buscar el script "Health" que tiene tu jugador
        Health playerHealth = player.GetComponent<Health>();

        if (playerHealth != null)
        {
            // Restar la vida usando tu función TakeDamage
            playerHealth.TakeDamage(attackDamage);

            // Imprimir en la consola para confirmar que funciona
            Debug.Log("¡El topo atacó y te hizo " + attackDamage + " de daño!");
        }
    }
}