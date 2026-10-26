using UnityEngine;

public class Coin : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica si el objeto que entró en contacto es el jugador
        if (collision.CompareTag("Player"))
        {
            // Busca automáticamente el gestor en la escena y activa la suma
            CoinManager gestor = FindObjectOfType<CoinManager>();
            if (gestor != null)
            {
                gestor.SumarMoneda();
            }

            // Destruye el objeto de la moneda del mapa al ser recolectada
            Destroy(gameObject);
        }
    }
}