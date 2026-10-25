using UnityEngine;

/// <summary>
/// Cicla los fotogramas de caminata de un aldeano. Las hojas "hero-walk" ya
/// vienen recortadas en frames de 32x32 (frente, espalda y perfil), así que el
/// NPC deja de deslizarse como una imagen fija y camina de verdad.
/// Mientras está quieto muestra el primer fotograma de su dirección actual,
/// que es lo que evita la sensación de "estar flotando".
/// </summary>
public class NpcAnimator : MonoBehaviour
{
    [Header("Fotogramas (los asigna el generador de escenas)")]
    public Sprite[] walkDown;
    public Sprite[] walkUp;
    public Sprite[] walkSide;

    [Header("Reproducción")]
    public float framesPerSecond = 9f;
    public bool sideFacesRight = true;

    private SpriteRenderer sr;
    private Sprite[] current;
    private float timer;
    private int frame;
    private Vector2 lastDirection = Vector2.down;

    public bool IsWalking { get; private set; }
    public Vector2 LastDirection { get { return lastDirection; } }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>Lo llama <see cref="NpcWanderer"/> cada frame con su dirección de avance.</summary>
    public void Animate(Vector2 direction, bool moving)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (direction.sqrMagnitude > 0.0001f) lastDirection = direction.normalized;

        PickSet(lastDirection);
        if (current == null || current.Length == 0) return;

        IsWalking = moving;

        if (moving)
        {
            timer += Time.deltaTime;
            float step = 1f / Mathf.Max(1f, framesPerSecond);
            int guard = 0;
            while (timer >= step && guard++ < 8)
            {
                timer -= step;
                frame = (frame + 1) % current.Length;
                Apply();
            }
        }
        else
        {
            timer = 0f;
            if (frame != 0)
            {
                frame = 0;
                Apply();
            }
        }

        if (current == walkSide && Mathf.Abs(lastDirection.x) > 0.01f)
            sr.flipX = sideFacesRight ? lastDirection.x < 0f : lastDirection.x > 0f;
    }

    void PickSet(Vector2 direction)
    {
        Sprite[] set;
        if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x))
            set = direction.y > 0f ? walkUp : walkDown;
        else
            set = walkSide;

        if (set == null || set.Length == 0) set = walkDown;
        if (set == null || set.Length == 0) set = walkSide;
        if (set == null || set.Length == 0) return;

        if (current != set)
        {
            current = set;
            frame = 0;
            timer = 0f;
            Apply();
        }
    }

    void Apply()
    {
        if (sr == null || current == null || current.Length == 0) return;
        int index = Mathf.Clamp(frame, 0, current.Length - 1);
        if (current[index] != null) sr.sprite = current[index];
    }
}
