using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class Health : MonoBehaviour
{
    public bool isPlayer = false;
    public bool IsAlive = true; 

    public int CurrentHealth = 100;
    public int MaxHealth = 100;
    public int maxHealth = 100;
    public int ShieldPoints = 0;

    public Action OnDied;
    public Action OnHealthChanged;

    public void TakeDamage(int amount) 
    {
        if (!IsAlive) return;

        CurrentHealth -= amount;
        Debug.Log(gameObject.name + " recibió daño. Vida restante: " + CurrentHealth);

        if (CurrentHealth <= 0)
        {
            IsAlive = false;
            
            // Si el objeto es el jugador, reinicia la escena
            if (isPlayer || gameObject.CompareTag("Player"))
            {
                Debug.Log("El jugador ha muerto. Reiniciando...");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else 
            {
                // Si es un enemigo, lo destruye
                Destroy(gameObject);
            }
        }
    }

    public void TakeDamage(float amount) 
    {
        TakeDamage(Mathf.RoundToInt(amount));
    }

    public void Heal(int amount) {}
    public void AgregarVida(int amount) {}
    public void AddShield(int amount) {}
    public void ResetHealth() {}
    public void SetHealth(int amount) {}
}