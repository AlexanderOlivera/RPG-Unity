using UnityEngine;

/// <summary>
/// NPC interactivo: muestra un aviso al acercarse, abre un diálogo con E y
/// permite comerciar con C. El texto puede venir de la base de datos de diálogos
/// o estar embebido en el propio NPC.
/// </summary>
public class NpcInteractable : MonoBehaviour
{
    public string npcName = "Anciano";
    public string role = "Habitante";
    [TextArea(2, 5)]
    public string[] lines;
    public float interactRange = 2.2f;
    public float bobAmplitude = 0.03f;
    public bool bob = true;

    [Header("Comercio (las monedas sirven para algo)")]
    public string tradeItemName = "";
    [TextArea(1, 3)]
    public string tradeDescription = "";
    public int tradePrice = 0;
    public TradeEffect tradeEffect = TradeEffect.Heal;
    public int tradePower = 40;

    /// <summary>NPC con el que se está hablando ahora mismo (lo usa la UI).</summary>
    public static NpcInteractable Active { get; private set; }

    public bool HasTrade { get { return tradePrice > 0 && !string.IsNullOrEmpty(tradeItemName); } }

    private bool inRange;
    private bool talked;
    private Vector3 startPosition;
    private PlayerController cachedPlayer;

    void Start()
    {
        startPosition = transform.position;
        if (lines == null || lines.Length == 0)
            lines = new[] { "La corrupción avanza... encuentra los cuatro fragmentos del sello." };
    }

    void Update()
    {
        if (GameManager.IsPaused || UIRoot.DialogueOpen) return;

        if (cachedPlayer == null) cachedPlayer = Object.FindFirstObjectByType<PlayerController>();
        if (cachedPlayer == null) return;

        if (bob)
            transform.position = startPosition + Vector3.up * Mathf.Sin(Time.time * 1.6f) * bobAmplitude;

        bool nowInRange = Vector2.Distance(transform.position, cachedPlayer.transform.position) <= interactRange;
        if (nowInRange != inRange)
        {
            inRange = nowInRange;
            if (UIRoot.Instance != null)
                UIRoot.Instance.ShowPrompt(inRange ? PromptText() : string.Empty);
        }

        if (inRange && Input.GetKeyDown(KeyCode.E)) Talk();
    }

    string PromptText()
    {
        if (HasTrade) return "[E] Hablar con " + npcName + "   ·   [C] Comerciar (" + tradePrice + " monedas)";
        return "[E] Hablar con " + npcName;
    }

    public void Talk()
    {
        Active = this;
        if (!talked)
        {
            talked = true;
            GameSession.NpcsTalked++;
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.Item);
        if (UIRoot.Instance != null)
        {
            UIRoot.Instance.ShowDialogue(npcName, lines);
            UIRoot.Instance.ShowPrompt(string.Empty);
            UIRoot.Instance.RefreshTradeOption();
        }
        inRange = false;
    }

    /// <summary>Compra el objeto de este NPC. Devuelve true si la transacción se hizo.</summary>
    public bool Trade()
    {
        if (!HasTrade)
        {
            if (UIRoot.Instance != null) UIRoot.Instance.Toast(npcName + " no tiene nada que vender.");
            return false;
        }

        if (GameSession.Coins < tradePrice)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.Hurt);
            if (UIRoot.Instance != null)
                UIRoot.Instance.Toast("Faltan " + (tradePrice - GameSession.Coins) + " monedas para " + tradeItemName);
            return false;
        }

        GameSession.SpendCoins(tradePrice);
        GameSession.Trades++;
        GameSession.AddItem(tradeItemName, 1);

        string effect = PlayerBuffs.Apply(tradeEffect, tradePower);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.Item);
        if (UIRoot.Instance != null)
        {
            UIRoot.Instance.Toast("Compraste " + tradeItemName + " · " + effect);
            UIRoot.Instance.RefreshTradeOption();
        }
        GameManager.NotifyChanged();
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
