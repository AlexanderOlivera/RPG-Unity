using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public int maxHealth = 30; // Vida inicial (3 corazones)
    public int absoluteMaxHealth = 50; // Límite máximo con gemas (5 corazones)
    private int currentHealth;

    [Header("Efectos (Enemigos)")]
    public GameObject explosionPrefab;
    public GameObject coinPrefab;
    public GameObject gemPrefab; // Casilla para la gema
    [Range(0f, 1f)] public float gemDropChance = 0.15f; // 15% de probabilidad de soltar gema

    [Header("UI (Solo Jugador)")]
    public Image[] corazones;
    public Sprite corazonLleno;
    public Sprite corazonVacio;

    public GameOverManager gestorGameOver;

    void Start()
    {
        currentHealth = maxHealth;
        ActualizarCorazones();
    }

    public void TakeDamage(int damage)
    {
        if (Time.timeScale == 0f) return;

        currentHealth -= damage;
        ActualizarCorazones();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Nueva función para sumar vida al recoger la gema
    public void AgregarVida(int cantidad)
    {
        currentHealth += cantidad;
        if (currentHealth > absoluteMaxHealth)
        {
            currentHealth = absoluteMaxHealth; // Evita que pase de 5 corazones
        }
        ActualizarCorazones();
    }

    void ActualizarCorazones()
    {
        if (corazones.Length == 0) return;
        int corazonesLlenos = currentHealth / 10;
        int corazonesBase = maxHealth / 10; // Siempre son 3 corazones base

        for (int i = 0; i < corazones.Length; i++)
        {
            if (i < corazonesLlenos)
            {
                // Tiene vida: Mostrar el corazón lleno
                corazones[i].gameObject.SetActive(true);
                corazones[i].sprite = corazonLleno;
            }
            else
            {
                // No tiene vida en este espacio
                if (i < corazonesBase)
                {
                    // Es uno de los 3 corazones iniciales: Mostrar marca vacía
                    corazones[i].gameObject.SetActive(true);
                    corazones[i].sprite = corazonVacio;
                }
                else
                {
                    // Son el 4to o 5to corazón: Ocultarlos por completo
                    corazones[i].gameObject.SetActive(false);
                }
            }
        }
    }

    void Die()
    {
        if (gameObject.CompareTag("Player"))
        {
            if (gestorGameOver != null) gestorGameOver.MostrarGameOver();
            gameObject.SetActive(false);
        }
        else
        {
            if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            if (coinPrefab != null) Instantiate(coinPrefab, transform.position, Quaternion.identity);

            // Dropeo por probabilidad: Solo suelta la gema si el valor aleatorio es menor o igual al 15%
            if (gemPrefab != null && Random.value <= gemDropChance)
            {
                Instantiate(gemPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}