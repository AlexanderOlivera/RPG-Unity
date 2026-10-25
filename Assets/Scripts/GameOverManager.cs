using UnityEngine;
using UnityEngine.SceneManagement; // Requerido para recargar escenas

public class GameOverManager : MonoBehaviour
{
    public GameObject panelGameOver;

    public void MostrarGameOver()
    {
        panelGameOver.SetActive(true); // Hace visible el panel gris
        Time.timeScale = 0f; // Congela físicamente todo el juego
    }

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f; // Descongela el tiempo antes de recargar
        // Recarga la escena en la que estás actualmente
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        // Cambia "Menu" por el nombre exacto de tu escena inicial cuando la crees
        SceneManager.LoadScene("Menu");
    }
}