using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Configuración del Enemigo")]
    public Transform player;
    public float speed = 2.5f;
    public float attackRange = 1.1f;
    public int attackDamage = 5;
    public float attackCooldown = 1.5f;

    // Referencias a los componentes internos
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    // Temporizador oculto para el cooldown de ataque
    private float nextAttackTime = 0f;

    void Start()
    {
        // Conectar los componentes en cuanto el enemigo aparece en la escena
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Medida de seguridad: Si no hay jugador asignado, el enemigo no hace nada
        if (player == null) return;

        // Calcular a qué distancia está el jugador
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Si el jugador está más lejos que el rango de ataque, lo perseguimos
        if (distanceToPlayer > attackRange)
        {
            // 1. Mover al enemigo usando físicas (Rigidbody) para que respete los muros
            Vector2 nuevaPosicion = Vector2.MoveTowards(rb.position, player.position, speed * Time.deltaTime);
            rb.MovePosition(nuevaPosicion);

            // 2. Calcular la dirección exacta hacia la que caminamos
            Vector2 direction = (player.position - transform.position).normalized;

            // 3. Enviar las variables en MAYÚSCULAS al Animator
            animator.SetFloat("X", direction.x);
            animator.SetFloat("Y", direction.y);
            animator.SetFloat("Speed", speed);

            // 4. Lógica para voltear el sprite según el lado al que camine
            if (direction.x < 0)
                spriteRenderer.flipX = true;
            else if (direction.x > 0)
                spriteRenderer.flipX = false;
        }
        else
        {
            // 5. El enemigo está pegado al jugador: detener la animación de caminar
            animator.SetFloat("Speed", 0);

            // 6. Revisar si ya pasó el tiempo de recarga (Cooldown) para poder dar el golpe
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
        // (Opcional) Si le agregas animación de ataque al enemigo, actívala aquí:
        // animator.SetTrigger("Attack");

        // Buscar el script "Health" en el jugador
        Health playerHealth = player.GetComponent<Health>();

        // Si el jugador tiene vida, hacerle daño
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);

            // Imprimir en la consola para comprobar que funcionó
            Debug.Log("¡El enemigo te alcanzó y te hizo " + attackDamage + " de daño!");
        }
    }
}