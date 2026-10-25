using UnityEngine;

/// <summary>
/// Espora de la corrupción: enemigo invocado por el Guardián. Es rápida y frágil,
/// y al morir estalla en una nube de esporas que araña al jugador si está pegado.
/// Eso obliga a pelear a media distancia en lugar de aguantar el cuerpo a cuerpo.
/// </summary>
public class SporeBurst : MonoBehaviour
{
    public int burstDamage = 7;
    public float burstRadius = 1.7f;
    public float burstDelay = 0.12f;

    private Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        if (health != null) health.OnDied += HandleDied;
    }

    void OnDestroy()
    {
        if (health != null) health.OnDied -= HandleDied;
    }

    void HandleDied(Health h)
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        if (Vector2.Distance(transform.position, player.transform.position) > burstRadius) return;

        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth == null) return;

        int damage = Mathf.Max(1, Mathf.RoundToInt(burstDamage * Difficulty.DamageMultiplier));
        if (playerHealth.TakeDamage(damage) && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxId.Hurt);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.75f, 0.35f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, burstRadius);
    }
}
