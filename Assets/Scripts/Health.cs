using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public int maxHealth = 30;
    private int currentHealth;

    [Header("Efectos (Enemigos)")]
    public GameObject explosionPrefab;

    [Header("UI (Solo Jugador)")]
    public Image[] corazones;
    public Sprite corazonLleno;
    public Sprite corazonVacio;

    // Nueva variable para conectar el Game Over
    public GameOverManager gestorGameOver;

    void Start()
    {
        currentHealth = maxHealth;
        ActualizarCorazones();
    }

    public void TakeDamage(int damage)
    {
        // BLOQUEO DE PAUSA: Evita recibir daño si el tiempo está congelado
        if (Time.timeScale == 0f) return;

        currentHealth -= damage;
        ActualizarCorazones();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void ActualizarCorazones()
    {
        if (corazones.Length == 0) return;
        int corazonesLlenos = currentHealth / 10;
        for (int i = 0; i < corazones.Length; i++)
        {
            if (i < corazonesLlenos) corazones[i].sprite = corazonLleno;
            else corazones[i].sprite = corazonVacio;
        }
    }

    void Die()
    {
        // 1. Si es el jugador el que muere
        if (gameObject.CompareTag("Player"))
        {
            if (gestorGameOver != null)
            {
                gestorGameOver.MostrarGameOver();
            }

            // APAGA AL JUGADOR: Desactiva todos sus scripts para que no pueda hacer nada más
            gameObject.SetActive(false);
        }
        // 2. Si es un enemigo el que muere
        else
        {
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}