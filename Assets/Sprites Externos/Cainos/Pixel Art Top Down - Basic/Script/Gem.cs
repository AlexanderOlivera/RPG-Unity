using UnityEngine;

public class Gem : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Busca el script Health del jugador y le suma 10 de vida (1 corazón)
            Health saludJugador = collision.GetComponent<Health>();
            if (saludJugador != null)
            {
                saludJugador.AgregarVida(10);
            }

            Destroy(gameObject);
        }
    }
}