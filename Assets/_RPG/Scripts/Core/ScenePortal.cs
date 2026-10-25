using UnityEngine;

/// <summary>
/// Portal/transición entre escenarios. Puede exigir fragmentos del sello o la
/// derrota del jefe antes de permitir el paso.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ScenePortal : MonoBehaviour
{
    public string targetScene;
    public int requiredFragments = 0;
    public bool requiresBossDefeated = false;
    public string label = "Salida";

    private bool loading;
    private float armTime;

    void Awake()
    {
        // Margen de gracia: si el jugador aparece solapando el portal al cargar la
        // escena, no se dispara la transición hasta que salga y vuelva a entrar.
        armTime = Time.time + 1f;
    }

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (loading || other == null) return;
        if (Time.time < armTime) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (string.IsNullOrEmpty(targetScene)) return;

        if (requiresBossDefeated && !GameSession.BossDefeated)
        {
            if (UIRoot.Instance != null) UIRoot.Instance.Toast("El Guardián sigue en pie.");
            return;
        }

        if (GameSession.Fragments < requiredFragments)
        {
            if (UIRoot.Instance != null)
                UIRoot.Instance.Toast("Necesitas " + requiredFragments + " fragmentos (" + GameSession.Fragments + "/" + requiredFragments + ")");
            return;
        }

        loading = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.Item);
        if (UIRoot.Instance != null) UIRoot.Instance.Toast("Entrando en " + targetScene + "...");

        GameManager gm = GameManager.Instance;
        if (gm != null) gm.LoadScene(targetScene);
    }
}
