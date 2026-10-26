using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz de juego: HUD (vida, monedas, puntaje, enemigos, fragmentos),
/// mochila, diálogos con comercio, barra del jefe, avisos, pausa, derrota y
/// victoria con créditos. Construida con el kit visual compartido <see cref="UiKit"/>.
/// </summary>
public class UIRoot : MonoBehaviour
{
    public static UIRoot Instance { get; private set; }
    public static bool DialogueOpen { get; private set; }

    public static void ResetUiState()
    {
        DialogueOpen = false;
    }

    // HUD
    private Text hpText, coinText, scoreText, killText, fragmentText, objectiveText, promptText, toastText;
    private Text buffText;
    private Image healthFill;
    private GameObject buffPanel;

    // Barra del jefe
    private GameObject bossPanel;
    private Image bossFill;
    private Text bossLabel, bossHint, bossValueText;
    private Health bossHealth;
    private float bossScanTimer;

    // Paneles
    private GameObject inventoryPanel, dialoguePanel, pausePanel, gameOverPanel, victoryPanel, bannerPanel;
    private GameObject promptPanel, toastPanel;

    // Mochila
    private RectTransform slotRoot;
    private Text backpackCoinText;

    // Diálogo y comercio
    private Text dialogueSpeakerText, dialogueBodyText, dialogueTradeText;
    private Button dialogueTradeButton;
    private Text dialogueTradeLabel;
    private string[] dialogueLines;
    private int dialogueIndex;
    private string dialogueSpeaker;

    // Victoria
    private Text victoryStats, victoryEpilogue;

    private float toastUntil;
    private float bannerUntil;
    private float buffRefreshTimer;
    private Health playerHealth;
    private Transform playerTransform;

    private const float BossRevealRange = 30f;

    void Awake()
    {
        Instance = this;
        BuildUi();
        GameManager.OnSessionChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        GameManager.OnSessionChanged -= Refresh;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        PlayerBuffs.Tick(Time.deltaTime);

        if (toastPanel != null && toastPanel.activeSelf && toastUntil > 0f && Time.unscaledTime > toastUntil)
        {
            toastText.text = string.Empty;
            toastUntil = 0f;
            toastPanel.SetActive(false);
        }

        if (bannerPanel != null && bannerPanel.activeSelf && bannerUntil > 0f && Time.unscaledTime > bannerUntil)
            bannerPanel.SetActive(false);

        UpdateHealthDisplay();
        UpdateBossBar();

        // El chip de mejoras se refresca a ritmo bajo: así la cuenta atrás de la
        // bruma avanza y el panel desaparece cuando expira (antes se quedaba fijo).
        buffRefreshTimer -= Time.unscaledDeltaTime;
        if (buffRefreshTimer <= 0f)
        {
            buffRefreshTimer = 0.25f;
            RefreshBuffChip();
        }

        if (GameManager.IsGameOver || GameManager.IsVictory) return;

        if (DialogueOpen && Input.GetKeyDown(KeyCode.C) && NpcInteractable.Active != null)
            NpcInteractable.Active.Trade();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (DialogueOpen) CloseDialogue();
            else if (inventoryPanel != null && inventoryPanel.activeSelf) ToggleInventory();
            else GameManager.TogglePause();
        }

        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab)) ToggleInventory();
    }

    // ---------- API pública usada por otros sistemas ----------

    public void Toast(string message)
    {
        if (toastText == null) return;
        toastText.text = message;
        toastUntil = Time.unscaledTime + 2.4f;
        if (toastPanel != null) toastPanel.SetActive(true);
    }

    public void ShowPrompt(string message)
    {
        if (promptText != null) promptText.text = message;
        if (promptPanel != null) promptPanel.SetActive(!string.IsNullOrEmpty(message));
    }

    public void ShowBanner(string title, string body, float seconds)
    {
        if (bannerPanel == null) return;
        bannerPanel.SetActive(true);
        if (bannerTitleText != null) bannerTitleText.text = title;
        if (bannerBodyText != null) bannerBodyText.text = body;
        bannerUntil = Time.unscaledTime + Mathf.Max(2f, seconds);
    }

    public void ShowDialogue(string speaker, string[] lines)
    {
        if (lines == null || lines.Length == 0) return;
        dialogueSpeaker = speaker;
        dialogueLines = lines;
        dialogueIndex = 0;
        DialogueOpen = true;
        Time.timeScale = 0f;
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        RenderDialogueLine();
        RefreshTradeOption();
        ShowPrompt(string.Empty);
    }

    public void NextDialogue()
    {
        dialogueIndex++;
        if (dialogueLines == null || dialogueIndex >= dialogueLines.Length) CloseDialogue();
        else RenderDialogueLine();
    }

    public void CloseDialogue()
    {
        DialogueOpen = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (!GameManager.IsPaused && !GameManager.IsGameOver && !GameManager.IsVictory) Time.timeScale = 1f;
    }

    /// <summary>Muestra u oculta la opción de comerciar según el NPC activo.</summary>
    public void RefreshTradeOption()
    {
        if (dialogueTradeButton == null) return;

        NpcInteractable npc = NpcInteractable.Active;
        bool show = npc != null && npc.HasTrade;
        dialogueTradeButton.gameObject.SetActive(show);

        if (!show)
        {
            if (dialogueTradeText != null) dialogueTradeText.text = string.Empty;
            return;
        }

        if (dialogueTradeLabel != null)
            dialogueTradeLabel.text = "COMERCIAR  [C]  ·  " + npc.tradePrice + " monedas";

        if (dialogueTradeText != null)
        {
            string description = string.IsNullOrEmpty(npc.tradeDescription) ? npc.tradeItemName : npc.tradeDescription;
            dialogueTradeText.text = "<color=#8CCB7A>" + npc.tradeItemName + "</color> · " + description;
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;
        bool show = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(show);
        if (show) RefreshBackpack();
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverStats != null)
            gameOverStats.text = "Puntaje: " + GameSession.Score + "\nMonedas: " + GameSession.Coins +
                                 "\nEnemigos derrotados: " + GameSession.Kills;
    }

    public void ShowVictory()
    {
        if (victoryPanel != null) victoryPanel.SetActive(true);

        if (victoryEpilogue != null) victoryEpilogue.text = Ending.Epilogue();
        if (victoryStats != null) victoryStats.text = Ending.Stats();
        if (victoryTitleText != null) victoryTitleText.text = Ending.Title;
    }

    public void Refresh()
    {
        if (scoreText != null) scoreText.text = "PUNTOS " + GameSession.Score;
        if (coinText != null) coinText.text = "MONEDAS " + GameSession.Coins;
        if (killText != null) killText.text = "ENEMIGOS " + GameSession.Kills;
        if (fragmentText != null) fragmentText.text = "FRAGMENTOS " + GameSession.Fragments + "/4";
        if (objectiveText != null)
            objectiveText.text = QuestTracker.Instance != null
                ? QuestTracker.Instance.ObjectiveText()
                : "Reúne los 4 fragmentos del sello.";

        RefreshBuffChip();
        if (inventoryPanel != null && inventoryPanel.activeSelf) RefreshBackpack();

        if (pausePanel != null) pausePanel.SetActive(GameManager.IsPaused && !GameManager.IsGameOver && !GameManager.IsVictory);
        UpdateHealthDisplay();
    }

    void RefreshBuffChip()
    {
        string summary = PlayerBuffs.Summary();
        if (buffPanel != null) buffPanel.SetActive(!string.IsNullOrEmpty(summary));
        if (buffText != null) buffText.text = summary;
    }

    void UpdateHealthDisplay()
    {
        if (hpText == null) return;

        if (playerHealth == null)
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
                playerTransform = player.transform;
            }
        }

        if (playerHealth == null) return;

        int current = Mathf.Max(0, playerHealth.CurrentHealth);
        int max = Mathf.Max(1, playerHealth.MaxHealth);
        int shield = playerHealth.ShieldPoints;

        string label = current + " / " + max;
        if (shield > 0) label += "   +" + shield;
        if (PlayerBuffs.ExtraLives > 0) label += "   ♥x" + PlayerBuffs.ExtraLives;
        hpText.text = label;

        if (healthFill != null) healthFill.fillAmount = Mathf.Clamp01((float)current / max);
    }

    void UpdateBossBar()
    {
        if (bossPanel == null) return;

        if (bossHealth == null)
        {
            bossScanTimer -= Time.unscaledDeltaTime;
            if (bossScanTimer <= 0f)
            {
                bossScanTimer = 1f;
                EnemyAI[] enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
                for (int i = 0; i < enemies.Length; i++)
                {
                    if (enemies[i] == null || !enemies[i].isBoss) continue;
                    bossHealth = enemies[i].GetComponent<Health>();
                    break;
                }
            }
        }

        bool visible = false;
        if (bossHealth != null && bossHealth.IsAlive)
        {
            if (playerTransform == null)
            {
                PlayerController player = Object.FindFirstObjectByType<PlayerController>();
                if (player != null) playerTransform = player.transform;
            }

            float distance = playerTransform != null
                ? Vector2.Distance(playerTransform.position, bossHealth.transform.position)
                : 0f;
            visible = distance <= BossRevealRange;
        }

        if (bossPanel.activeSelf != visible) bossPanel.SetActive(visible);
        if (!visible) return;

        int current = Mathf.Max(0, bossHealth.CurrentHealth);
        int max = Mathf.Max(1, bossHealth.MaxHealth);
        if (bossFill != null) bossFill.fillAmount = Mathf.Clamp01((float)current / max);
        if (bossLabel != null) bossLabel.text = "GUARDIÁN DEL SELLO";
        if (bossValueText != null) bossValueText.text = current + " / " + max;
        if (bossHint != null) bossHint.text = "Corrupción activa · invoca esporas de raíz negra";
    }

    void RenderDialogueLine()
    {
        if (dialogueSpeakerText != null) dialogueSpeakerText.text = dialogueSpeaker;
        if (dialogueBodyText != null && dialogueLines != null && dialogueIndex < dialogueLines.Length)
            dialogueBodyText.text = dialogueLines[dialogueIndex];
    }

    // ---------- Mochila ----------

    void RefreshBackpack()
    {
        if (slotRoot == null) return;

        for (int i = slotRoot.childCount - 1; i >= 0; i--)
            Destroy(slotRoot.GetChild(i).gameObject);

        List<KeyValuePair<string, int>> items = new List<KeyValuePair<string, int>>(GameSession.Inventory);
        items.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

        const int totalSlots = 15;
        for (int i = 0; i < totalSlots; i++)
        {
            bool filled = i < items.Count;
            Color fill = filled ? new Color(0.16f, 0.12f, 0.09f, 1f) : new Color(0.11f, 0.09f, 0.08f, 1f);
            Color border = filled ? UiKit.Gold : new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.28f);

            RectTransform slot = UiKit.UiPanel("Slot_" + i, slotRoot, fill, border, 2f);
            if (!filled) continue;

            string itemName = items[i].Key;
            int count = items[i].Value;

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(slot, false);
            Image icon = iconGo.GetComponent<Image>();
            icon.sprite = UiIcons.Get(UiIcons.KeyFor(itemName));
            icon.color = Color.white;
            icon.raycastTarget = false;
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            UiKit.Place(iconRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 6f), new Vector2(48f, 48f));

            Text countText = UiKit.UiText("Count", slot, "x" + count, 15, TextAnchor.MiddleRight, UiKit.GoldBright, true);
            UiKit.Place(countText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-6f, 4f), new Vector2(50f, 18f));

            Text nameText = UiKit.UiText("Name", slot, ShortName(itemName), 11, TextAnchor.MiddleCenter, UiKit.ParchmentDim);
            UiKit.Place(nameText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 14f), new Vector2(80f, 16f));
        }

        if (backpackCoinText != null)
        {
            backpackCoinText.text = "MONEDAS " + GameSession.Coins +
                                    "      ·      GASTADAS " + GameSession.CoinsSpent +
                                    "      ·      COMPRAS " + GameSession.Trades;
        }
    }

    static string ShortName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return string.Empty;
        if (itemName.Length <= 12) return itemName;
        return itemName.Substring(0, 11) + ".";
    }

    // ---------- Construcción de la UI ----------

    private Text bannerTitleText, bannerBodyText, victoryTitleText;
    private Text inventoryText, dialogueBodyTextAlias, gameOverStats;

    static void Fill(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    void BuildUi()
    {
        Canvas canvas = UiKit.UiCanvas("Canvas", transform, 100);
        Transform root = canvas.transform;

        // ---------------- HUD: vitales ----------------
        RectTransform vitals = UiKit.UiPanel("VitalsPanel", root, UiKit.Ink, UiKit.Gold, 2f);
        UiKit.Place(vitals, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -24f), new Vector2(404f, 180f));

        Text vitalsTitle = UiKit.UiSection("VitalsTitle", vitals, "Vitales");
        UiKit.Place(vitalsTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(18f, -12f), new Vector2(200f, 22f));

        healthFill = UiKit.UiBar("HealthBar", vitals, new Vector2(368f, 30f),
            new Color(0.14f, 0.10f, 0.10f, 1f), UiKit.BloodBright, out hpText, "100 / 100");
        RectTransform barFrame = (RectTransform)healthFill.transform.parent;
        UiKit.Place(barFrame, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(18f, -40f), new Vector2(368f, 30f));

        coinText = UiKit.UiChip("CoinChip", vitals, new Vector2(178f, 32f), new Vector2(18f, -82f), UiKit.GoldBright);
        scoreText = UiKit.UiChip("ScoreChip", vitals, new Vector2(178f, 32f), new Vector2(208f, -82f), UiKit.Parchment);
        killText = UiKit.UiChip("KillChip", vitals, new Vector2(178f, 32f), new Vector2(18f, -122f), new Color(0.92f, 0.76f, 0.80f, 1f));
        fragmentText = UiKit.UiChip("FragmentChip", vitals, new Vector2(178f, 32f), new Vector2(208f, -122f), UiKit.MossBright);

        // Mejoras activas (compras a los aldeanos)
        RectTransform buff = UiKit.UiPanel("BuffPanel", root, new Color(0.10f, 0.14f, 0.09f, 0.92f), UiKit.MossBright, 1f);
        UiKit.Place(buff, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -212f), new Vector2(404f, 34f));
        buffText = UiKit.UiText("BuffText", buff, string.Empty, 16, TextAnchor.MiddleCenter, UiKit.MossBright, true);
        UiKit.Stretch(buffText.rectTransform, 6f);
        buffPanel = buff.gameObject;
        buffPanel.SetActive(false);

        // ---------------- HUD: objetivo ----------------
        RectTransform objectivePanel = UiKit.UiPanel("ObjectivePanel", root, UiKit.InkSoft, UiKit.Gold, 2f);
        UiKit.Place(objectivePanel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(24f, 24f), new Vector2(620f, 112f));

        Text objectiveTitle = UiKit.UiSection("ObjectiveTitle", objectivePanel, "Objetivo");
        UiKit.Place(objectiveTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(18f, -12f), new Vector2(240f, 22f));

        objectiveText = UiKit.UiText("ObjectiveText", objectivePanel, "Objetivo", 19, TextAnchor.UpperLeft, UiKit.Parchment);
        Fill(objectiveText.rectTransform, 18f, 12f, 18f, 40f);

        // ---------------- HUD: acciones ----------------
        Button inventoryButton = UiKit.UiButton("InventoryButton", root, "MOCHILA  [I]",
            new Vector2(220f, 50f), 19, UiKit.Steel, UiKit.Gold, ToggleInventory);
        UiKit.Place(inventoryButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(220f, 50f));

        Button pauseButton = UiKit.UiButton("PauseButton", root, "PAUSA  [ESC]",
            new Vector2(190f, 50f), 19, UiKit.Steel, UiKit.Gold, GameManager.TogglePause);
        UiKit.Place(pauseButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-256f, -24f), new Vector2(190f, 50f));

        // ---------------- HUD: barra del jefe ----------------
        RectTransform boss = UiKit.UiPanel("BossPanel", root, new Color(0.09f, 0.05f, 0.07f, 0.94f), UiKit.BloodBright, 2f);
        UiKit.Place(boss, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -24f), new Vector2(780f, 104f));

        Text bossTitle = UiKit.UiText("BossTitle", boss, "GUARDIÁN DEL SELLO", 20, TextAnchor.MiddleCenter, new Color(1f, 0.72f, 0.72f, 1f), true);
        UiKit.Place(bossTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -8f), new Vector2(740f, 24f));
        bossLabel = bossTitle;

        bossFill = UiKit.UiBar("BossBar", boss, new Vector2(716f, 26f),
            new Color(0.16f, 0.07f, 0.09f, 1f), new Color(0.86f, 0.24f, 0.30f, 1f), out bossValueText, "180 / 180");
        RectTransform bossBarFrame = (RectTransform)bossFill.transform.parent;
        UiKit.Place(bossBarFrame, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -36f), new Vector2(716f, 26f));

        Text bossSubtitle = UiKit.UiText("BossSubtitle", boss, string.Empty, 14, TextAnchor.MiddleCenter, UiKit.ParchmentDim);
        UiKit.Place(bossSubtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -70f), new Vector2(740f, 20f));
        bossHint = bossSubtitle;

        bossPanel = boss.gameObject;
        bossPanel.SetActive(false);
        // ---------------- Avisos ----------------
        promptPanel = UiKit.UiPanel("PromptPanel", root, new Color(0f, 0f, 0f, 0.72f),
            new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.55f), 1f).gameObject;
        UiKit.Place(promptPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(980f, 44f));
        promptText = UiKit.UiText("PromptText", promptPanel.transform, string.Empty, 20, TextAnchor.MiddleCenter, UiKit.GoldBright, true);
        UiKit.Stretch(promptText.rectTransform, 4f);
        promptPanel.SetActive(false);

        toastPanel = UiKit.UiPanel("ToastPanel", root, new Color(0f, 0f, 0f, 0.62f),
            new Color(UiKit.MossBright.r, UiKit.MossBright.g, UiKit.MossBright.b, 0.45f), 1f).gameObject;
        UiKit.Place(toastPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(980f, 36f));
        toastText = UiKit.UiText("ToastText", toastPanel.transform, string.Empty, 19, TextAnchor.MiddleCenter, UiKit.MossBright, true);
        UiKit.Stretch(toastText.rectTransform, 4f);
        toastPanel.SetActive(false);

        BuildBackpack(root);
        BuildDialogue(root);
        BuildPause(root);
        BuildGameOver(root);
        BuildVictory(root);
        BuildBanner(root);
    }

    /// <summary>Inventario con forma de mochila: correas, solapa, bolsillo y rejilla de objetos.</summary>
    void BuildBackpack(Transform root)
    {
        inventoryPanel = UiKit.UiBackdrop("InventoryPanel", root, new Color(0.02f, 0.03f, 0.05f, 0.84f), true).gameObject;
        RectTransform card = UiKit.UiPanel("BackpackCard", inventoryPanel.transform, UiKit.Ink, UiKit.Gold, 3f);
        UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(960f, 700f));

        Text title = UiKit.UiText("BackpackTitle", card, "MOCHILA", 36, TextAnchor.MiddleCenter, UiKit.GoldBright, true);
        UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -26f), new Vector2(800f, 46f));
        UiKit.UiRule("BackpackRule", card, 2f, 40f);

        // Correas laterales de la mochila
        Color strap = new Color(0.17f, 0.12f, 0.09f, 1f);
        Color strapBorder = new Color(0.33f, 0.24f, 0.16f, 1f);
        for (int i = 0; i < 2; i++)
        {
            RectTransform s = UiKit.UiPanel("Strap_" + i, card, strap, strapBorder, 2f);
            UiKit.Place(s, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(i == 0 ? -336f : 336f, -102f), new Vector2(46f, 450f));
        }

        // Cuerpo de la mochila
        RectTransform body = UiKit.UiPanel("BackpackBody", card, new Color(0.205f, 0.152f, 0.112f, 1f),
            new Color(0.42f, 0.31f, 0.20f, 1f), 3f);
        UiKit.Place(body, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -92f), new Vector2(660f, 470f));

        // Solapa superior con hebilla
        RectTransform flap = UiKit.UiPanel("BackpackFlap", body, new Color(0.255f, 0.19f, 0.135f, 1f),
            new Color(0.46f, 0.34f, 0.21f, 1f), 2f);
        UiKit.Place(flap, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, 0f), new Vector2(660f, 116f));

        RectTransform buckle = UiKit.UiPanel("BackpackBuckle", flap, new Color(0.78f, 0.63f, 0.30f, 1f),
            new Color(0.55f, 0.42f, 0.18f, 1f), 2f);
        UiKit.Place(buckle, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 6f), new Vector2(74f, 34f));

        Text flapLabel = UiKit.UiText("FlapLabel", flap, "OBJETOS", 20, TextAnchor.MiddleCenter,
            new Color(0.94f, 0.86f, 0.68f, 1f), true);
        UiKit.Place(flapLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(600f, 30f));

        // Bolsillo frontal con la rejilla de objetos
        RectTransform pocket = UiKit.UiPanel("BackpackPocket", body, new Color(0.165f, 0.125f, 0.095f, 1f),
            new Color(0.44f, 0.33f, 0.21f, 1f), 2f);
        UiKit.Place(pocket, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -130f), new Vector2(596f, 300f));

        GameObject slots = new GameObject("Slots", typeof(RectTransform), typeof(GridLayoutGroup));
        slots.transform.SetParent(pocket, false);
        RectTransform slotsRt = slots.GetComponent<RectTransform>();
        UiKit.Place(slotsRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -14f), new Vector2(470f, 272f));

        GridLayoutGroup grid = slots.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(84f, 84f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter;
        slotRoot = slotsRt;

        backpackCoinText = UiKit.UiText("BackpackCoins", card, string.Empty, 21, TextAnchor.MiddleCenter, UiKit.GoldBright, true);
        UiKit.Place(backpackCoinText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 96f), new Vector2(880f, 30f));

        Text hint = UiKit.UiText("BackpackHint", card,
            "Los aldeanos venden mejoras: acércate, habla con [E] y compra con [C].",
            17, TextAnchor.MiddleCenter, UiKit.ParchmentDim);
        UiKit.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 68f), new Vector2(880f, 24f));

        Button close = UiKit.UiButton("CloseBackpack", card, "CERRAR  [I]", new Vector2(240f, 48f), 18,
            UiKit.Steel, UiKit.Gold, ToggleInventory);
        UiKit.Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(240f, 48f));

        inventoryText = null;
        inventoryPanel.SetActive(false);
    }

    void BuildDialogue(Transform root)
    {
        dialoguePanel = UiKit.UiBackdrop("DialoguePanel", root, new Color(0f, 0f, 0f, 0.55f), true).gameObject;
        RectTransform card = UiKit.UiPanel("DialogueCard", dialoguePanel.transform, UiKit.Ink, UiKit.Gold, 3f);
        UiKit.Place(card, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 44f), new Vector2(1240f, 286f));

        RectTransform namePlate = UiKit.UiPanel("NamePlate", card, new Color(0.11f, 0.09f, 0.06f, 1f), UiKit.Gold, 2f);
        UiKit.Place(namePlate, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(40f, 4f), new Vector2(360f, 48f));
        dialogueSpeakerText = UiKit.UiText("Speaker", namePlate, "NPC", 22, TextAnchor.MiddleCenter, UiKit.GoldBright, true);
        UiKit.Stretch(dialogueSpeakerText.rectTransform, 8f);

        dialogueBodyText = UiKit.UiText("Body", card, string.Empty, 23, TextAnchor.UpperLeft, UiKit.Parchment);
        Fill(dialogueBodyText.rectTransform, 40f, 112f, 40f, 34f);
        dialogueBodyTextAlias = dialogueBodyText;

        dialogueTradeText = UiKit.UiText("TradeInfo", card, string.Empty, 16, TextAnchor.MiddleLeft, UiKit.MossBright);
        UiKit.Place(dialogueTradeText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(40f, 66f), new Vector2(700f, 24f));

        Text hint = UiKit.UiText("DialogueHint", card, "[E] continuar   ·   [C] comerciar   ·   [Esc] cerrar",
            15, TextAnchor.MiddleLeft, UiKit.ParchmentDim);
        UiKit.Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(40f, 34f), new Vector2(460f, 24f));

        dialogueTradeButton = UiKit.UiButton("TradeButton", card, "COMERCIAR  [C]",
            new Vector2(230f, 46f), 16, new Color(0.22f, 0.34f, 0.22f, 1f), UiKit.Gold, BuyFromActiveNpc);
        UiKit.Place(dialogueTradeButton.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 0f), new Vector2(-268f, 22f), new Vector2(230f, 46f));
        dialogueTradeLabel = dialogueTradeButton.transform.Find("TradeButton_Label") != null
            ? dialogueTradeButton.transform.Find("TradeButton_Label").GetComponent<Text>()
            : null;
        dialogueTradeButton.gameObject.SetActive(false);

        Button continueDialogue = UiKit.UiButton("ContinueDialogue", card, "CONTINUAR  ▸",
            new Vector2(230f, 46f), 18, UiKit.Moss, UiKit.Gold, NextDialogue);
        UiKit.Place(continueDialogue.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 0f), new Vector2(-28f, 22f), new Vector2(230f, 46f));

        dialoguePanel.SetActive(false);
    }

    void BuildPause(Transform root)
    {
        pausePanel = UiKit.UiBackdrop("PausePanel", root, new Color(0.02f, 0.03f, 0.05f, 0.78f), true).gameObject;
        RectTransform card = UiKit.UiPanel("PauseCard", pausePanel.transform, UiKit.Ink, UiKit.Gold, 3f);
        UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(580f, 500f));

        Text title = UiKit.UiText("PauseTitle", card, "PAUSA", 42, TextAnchor.MiddleCenter, UiKit.GoldBright, true);
        UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(480f, 54f));
        UiKit.UiRule("PauseRule", card, 2f, 60f);

        Button resume = UiKit.UiButton("ResumeButton", card, "CONTINUAR", new Vector2(400f, 56f), 20,
            UiKit.Moss, UiKit.Gold, ResumeGame);
        UiKit.Place(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(400f, 56f));

        Button restart = UiKit.UiButton("RestartButton", card, "REINICIAR ESCENARIO", new Vector2(400f, 56f), 20,
            UiKit.Steel, UiKit.Gold, RestartGame);
        UiKit.Place(restart.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(400f, 56f));

        Button credits = UiKit.UiButton("PauseCreditsButton", card, "CRÉDITOS", new Vector2(400f, 56f), 20,
            new Color(0.26f, 0.24f, 0.34f, 1f), UiKit.Gold, GoToCredits);
        UiKit.Place(credits.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -246f), new Vector2(400f, 56f));

        Button menu = UiKit.UiButton("PauseMenuButton", card, "MENÚ PRINCIPAL", new Vector2(400f, 56f), 20,
            UiKit.Blood, UiKit.Gold, GoToMainMenu);
        UiKit.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -314f), new Vector2(400f, 56f));

        Text controlsHelp = UiKit.UiText("ControlsHelp", card,
            "WASD: mover   ·   Espacio: cuerpo a cuerpo   ·   F: distancia\n" +
            "Shift: esquiva   ·   E: hablar   ·   C: comerciar   ·   I: mochila   ·   Esc: pausa",
            16, TextAnchor.MiddleCenter, UiKit.ParchmentDim);
        UiKit.Place(controlsHelp.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 24f), new Vector2(540f, 54f));

        pausePanel.SetActive(false);
    }

    void BuildGameOver(Transform root)
    {
        gameOverPanel = UiKit.UiBackdrop("GameOverPanel", root, new Color(0.10f, 0.02f, 0.02f, 0.88f), true).gameObject;
        RectTransform card = UiKit.UiPanel("GameOverCard", gameOverPanel.transform, UiKit.Ink, UiKit.BloodBright, 3f);
        UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(680f, 520f));

        Text title = UiKit.UiText("GameOverTitle", card, "HAS CAÍDO", 50, TextAnchor.MiddleCenter, UiKit.BloodBright, true);
        UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -28f), new Vector2(600f, 62f));
        UiKit.UiRule("GameOverRule", card, 2f, 70f);

        Text subtitle = UiKit.UiText("GameOverSubtitle", card,
            "La corrupción te alcanza... pero el sello aún puede restaurarse.", 19, TextAnchor.MiddleCenter, UiKit.ParchmentDim);
        UiKit.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -108f), new Vector2(600f, 32f));

        gameOverStats = UiKit.UiText("GameOverStats", card, string.Empty, 22, TextAnchor.MiddleCenter, UiKit.Parchment);
        UiKit.Place(gameOverStats.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -160f), new Vector2(600f, 140f));

        Button retry = UiKit.UiButton("RetryButton", card, "REINTENTAR", new Vector2(340f, 56f), 20,
            UiKit.Moss, UiKit.Gold, RetryFromGameOver);
        UiKit.Place(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(340f, 56f));

        Button menu = UiKit.UiButton("GameOverMenuButton", card, "MENÚ PRINCIPAL", new Vector2(340f, 56f), 20,
            UiKit.Steel, UiKit.Gold, GoToMainMenu);
        UiKit.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(340f, 56f));

        gameOverPanel.SetActive(false);
    }

    void BuildVictory(Transform root)
    {
        victoryPanel = UiKit.UiBackdrop("VictoryPanel", root, new Color(0.02f, 0.07f, 0.04f, 0.92f), true).gameObject;
        RectTransform card = UiKit.UiPanel("VictoryCard", victoryPanel.transform, UiKit.Ink, UiKit.GoldBright, 3f);
        UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 720f));

        victoryTitleText = UiKit.UiText("VictoryTitle", card, "EL SELLO RESPIRA DE NUEVO",
            40, TextAnchor.MiddleCenter, UiKit.GoldBright, true);
        UiKit.Place(victoryTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -26f), new Vector2(820f, 54f));
        UiKit.UiRule("VictoryRule", card, 2f, 60f);

        Text subtitle = UiKit.UiText("VictorySubtitle", card,
            "La Arboleda vuelve a respirar y la corrupción se disipa.", 18, TextAnchor.MiddleCenter, UiKit.ParchmentDim);
        UiKit.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -94f), new Vector2(820f, 26f));

        victoryEpilogue = UiKit.UiText("VictoryEpilogue", card, string.Empty, 19, TextAnchor.UpperCenter, UiKit.Parchment);
        UiKit.Place(victoryEpilogue.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -128f), new Vector2(800f, 340f));

        victoryStats = UiKit.UiText("VictoryStats", card, string.Empty, 19, TextAnchor.MiddleCenter, UiKit.MossBright);
        UiKit.Place(victoryStats.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -476f), new Vector2(820f, 66f));

        Button credits = UiKit.UiButton("VictoryCreditsButton", card, "VER CRÉDITOS", new Vector2(300f, 58f), 21,
            new Color(0.42f, 0.32f, 0.16f, 1f), UiKit.GoldBright, GoToCredits);
        UiKit.Place(credits.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(300f, 58f));

        Button playAgain = UiKit.UiButton("PlayAgainButton", card, "JUGAR DE NUEVO", new Vector2(300f, 52f), 19,
            UiKit.Moss, UiKit.Gold, PlayAgain);
        UiKit.Place(playAgain.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(-330f, 36f), new Vector2(300f, 52f));

        Button menu = UiKit.UiButton("VictoryMenuButton", card, "MENÚ PRINCIPAL", new Vector2(300f, 52f), 19,
            UiKit.Steel, UiKit.Gold, GoToMainMenu);
        UiKit.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(330f, 36f), new Vector2(300f, 52f));

        victoryPanel.SetActive(false);
    }

    void BuildBanner(Transform root)
    {
        bannerPanel = UiKit.UiPanel("BannerPanel", root, new Color(0.04f, 0.05f, 0.07f, 0.90f), UiKit.Gold, 2f).gameObject;
        UiKit.Place(bannerPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1000f, 200f));

        bannerTitleText = UiKit.UiText("BannerTitle", bannerPanel.transform, string.Empty, 26, TextAnchor.MiddleCenter, UiKit.GoldBright, true);
        UiKit.Place(bannerTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -22f), new Vector2(920f, 36f));
        UiKit.UiRule("BannerRule", bannerPanel.transform, 2f, 120f);

        bannerBodyText = UiKit.UiText("BannerBody", bannerPanel.transform, string.Empty, 19, TextAnchor.UpperCenter, UiKit.Parchment);
        UiKit.Place(bannerBodyText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -72f), new Vector2(920f, 110f));
        bannerPanel.SetActive(false);
    }

    // ---------- Acciones de botones ----------

    public void ResumeGame() { GameManager.SetPaused(false); }
    public void RestartGame() { if (GameManager.Instance != null) GameManager.Instance.RestartScene(); }
    public void GoToMainMenu() { if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu(); }
    public void RetryFromGameOver() { if (GameManager.Instance != null) GameManager.Instance.RestartCurrentFromCheckpoint(); }
    public void PlayAgain() { if (GameManager.Instance != null) GameManager.Instance.StartNewGame(); }
    public void GoToCredits() { if (GameManager.Instance != null) GameManager.Instance.LoadCredits(); }

    void BuyFromActiveNpc()
    {
        if (NpcInteractable.Active != null) NpcInteractable.Active.Trade();
    }
}
