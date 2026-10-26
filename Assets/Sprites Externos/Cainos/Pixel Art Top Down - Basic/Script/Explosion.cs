using UnityEngine;

public class Explosion : MonoBehaviour
{
    // Ajusta este tiempo para que coincida exactamente con lo que dura tu animación
    public float animationDuration = 0.5f;

    void Start()
    {
        // Se destruye automáticamente al terminar la animación
        Destroy(gameObject, animationDuration);
    }
}