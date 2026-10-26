using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject panelPausa;
    private bool juegoPausado = false;

    void Update()
    {
        // Detecta si el jugador presiona la tecla P
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (juegoPausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Pausar()
    {
        panelPausa.SetActive(true);
        Time.timeScale = 0f; // Congela físicas y animaciones
        juegoPausado = true;
    }

    public void Reanudar()
    {
        panelPausa.SetActive(false);
        Time.timeScale = 1f; // Restaura el flujo del tiempo
        juegoPausado = false;
    }
}