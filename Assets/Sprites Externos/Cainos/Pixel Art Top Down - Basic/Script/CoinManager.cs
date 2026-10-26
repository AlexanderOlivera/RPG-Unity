using UnityEngine;
using TMPro; // Requerido para modificar textos de TextMeshPro

public class CoinManager : MonoBehaviour
{
    // Referencia al texto de la interfaz donde se mostrará el número
    public TextMeshProUGUI textoMonedas;

    // Contador interno de las monedas recolectadas
    private int monedasTotales = 0;

    // Función que se activa cada vez que el jugador toca una moneda
    public void SumarMoneda()
    {
        monedasTotales += 1;
        textoMonedas.text = monedasTotales.ToString(); // Actualiza el texto en pantalla
    }
}