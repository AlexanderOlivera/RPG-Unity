using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 30;
    private int currentHealth;

    void Start()
    {
        // Al iniciar, la vida actual es igual a la vida máxima
        currentHealth = maxHealth;
    }

    // Esta función debe ser "public" para que la espada del jugador pueda llamarla
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log(gameObject.name + " recibió " + damage + " de daño. Vida restante: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " ha sido liberado de la corrupción.");
        // Por ahora, destruimos el objeto del enemigo de la escena
        Destroy(gameObject);
    }
}