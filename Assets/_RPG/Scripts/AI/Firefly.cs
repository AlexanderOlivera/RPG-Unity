using UnityEngine;

/// <summary>
/// Luciérnaga ambiental: flota en círculos lentos y parpadea. Es el detalle que
/// hace que un bosque se sienta vivo aunque el jugador esté quieto.
/// </summary>
public class Firefly : MonoBehaviour
{
    public float radius = 1.4f;
    public float speed = 0.55f;
    public float heightFactor = 0.55f;
    public float minAlpha = 0.15f;
    public float maxAlpha = 0.95f;

    private Vector3 origin;
    private float phase;
    private SpriteRenderer sr;

    void Start()
    {
        origin = transform.position;
        phase = Random.value * 20f;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float t = Time.time * speed + phase;
        Vector3 offset = new Vector3(Mathf.Sin(t * 0.9f) * radius, Mathf.Cos(t * 0.63f + 1.7f) * radius * heightFactor, 0f);
        transform.position = origin + offset;

        if (sr != null)
        {
            float blink = (Mathf.Sin(t * 2.1f) + 1f) * 0.5f;
            Color c = sr.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, blink);
            sr.color = c;
        }
    }
}
