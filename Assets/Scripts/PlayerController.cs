using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    private SpriteRenderer spriteRenderer; // Referencia al gráfico

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // Se enlaza al inicio
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Control del Animator
        if (movement != Vector2.zero)
        {
            animator.SetFloat("X", movement.x);
            animator.SetFloat("Y", movement.y);
        }

        // Lógica para voltear el sprite
        if (movement.x < 0)
        {
            spriteRenderer.flipX = true; // Mira a la izquierda
        }
        else if (movement.x > 0)
        {
            spriteRenderer.flipX = false; // Mira a la derecha (normal)
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement.normalized * speed * Time.fixedDeltaTime);
    }
}