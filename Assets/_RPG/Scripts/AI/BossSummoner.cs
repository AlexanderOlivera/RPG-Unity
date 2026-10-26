using UnityEngine;

/// <summary>
/// Habilidad del jefe final: invoca esporas de la corrupción mientras pelea.
/// Las esporas son enemigos reales (EnemyAI) que persiguen al jugador, así que
/// el Guardián se convierte en una pelea de dos frentes.
/// </summary>
public class BossSummoner : MonoBehaviour
{
    [Header("Invocación")]
    public GameObject summonPrefab;
    public float firstSummonDelay = 2.5f;
    public float interval = 8f;
    public int perWave = 2;
    public int maxAlive = 6;
    public float spawnRadius = 2.8f;
    public float engageRange = 16f;

    private Health health;
    private float nextSummon;
    private Transform summonsRoot;
    private bool warned;

    void Start()
    {
        health = GetComponent<Health>();
        nextSummon = Time.time + firstSummonDelay;

        GameObject rootGo = GameObject.Find("Invocaciones");
        if (rootGo == null) rootGo = new GameObject("Invocaciones");
        summonsRoot = rootGo.transform;
    }

    void Update()
    {
        if (summonPrefab == null || health == null || !health.IsAlive) return;
        if (GameManager.IsPaused || UIRoot.DialogueOpen) return;
        if (Time.time < nextSummon) return;

        if (CountAlive() >= maxAlive)
        {
            nextSummon = Time.time + 1.5f;
            return;
        }

        nextSummon = Time.time + interval;
        Summon();
    }

    int CountAlive()
    {
        if (summonsRoot == null) return 0;
        int count = 0;
        for (int i = 0; i < summonsRoot.childCount; i++)
            if (summonsRoot.GetChild(i) != null) count++;
        return count;
    }

    void Summon()
    {
        Transform player = FindPlayer();
        if (player == null) return;
        if (Vector2.Distance(transform.position, player.position) > engageRange) return;

        if (!warned)
        {
            warned = true;
            if (UIRoot.Instance != null) UIRoot.Instance.Toast("¡El Guardián invoca esporas de la corrupción!");
        }

        for (int i = 0; i < perWave; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) *
                             Random.Range(spawnRadius * 0.55f, spawnRadius);

            GameObject spore = Instantiate(summonPrefab, transform.position + offset, Quaternion.identity);
            spore.name = "Espora_" + Time.frameCount + "_" + i;
            if (summonsRoot != null) spore.transform.SetParent(summonsRoot, true);

            EnemyAI ai = spore.GetComponent<EnemyAI>();
            if (ai != null) ai.player = player;
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.EnemyDeath);
    }

    Transform FindPlayer()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        return player != null ? player.transform : null;
    }
}
