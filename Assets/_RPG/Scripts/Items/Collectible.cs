using UnityEngine;

public enum CollectibleKind
{
    Coin = 0,
    Gem = 1,
    Heart = 2,
    Fragment = 3
}

/// <summary>
/// Coleccionable: monedas, gemas, corazones y fragmentos del sello.
/// Tiene una bandera para impedir que el mismo objeto sume puntos dos veces.
/// </summary>
public class Collectible : MonoBehaviour
{
    public CollectibleKind kind = CollectibleKind.Coin;
    public int amount = 1;
    public int healAmount = 25;
    public string displayName = "Moneda";
    public float bobAmplitude = 0.08f;
    public float bobSpeed = 2f;

    private bool collected;
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
        if (kind == CollectibleKind.Coin) displayName = InventorySystem.CoinItem;
        else if (kind == CollectibleKind.Gem) displayName = InventorySystem.GemItem;
        else if (kind == CollectibleKind.Heart) displayName = InventorySystem.HeartItem;
        else displayName = InventorySystem.FragmentItem;
    }

    void Update()
    {
        if (collected) return;
        transform.position = startPosition + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other == null) return;
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        Collect(player.GetComponent<Health>());
    }

    void Collect(Health playerHealth)
    {
        collected = true;

        switch (kind)
        {
            case CollectibleKind.Coin:
                ScoreSystem.AwardCoin();
                InventorySystem.AddCoin();
                break;
            case CollectibleKind.Gem:
                ScoreSystem.AwardGem();
                InventorySystem.AddGem();
                break;
            case CollectibleKind.Heart:
                InventorySystem.AddHeart();
                if (playerHealth != null) playerHealth.Heal(healAmount);
                break;
            case CollectibleKind.Fragment:
                ScoreSystem.AwardFragment();
                InventorySystem.AddFragment();
                break;
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.Item);
        if (UIRoot.Instance != null) UIRoot.Instance.Toast("+" + displayName);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        Destroy(gameObject, 0.05f);
    }
}
