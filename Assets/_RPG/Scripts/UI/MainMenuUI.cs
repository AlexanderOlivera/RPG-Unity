using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú principal: Jugar, Opciones (volumen real + dificultad), Créditos y Salir.
/// Construye su propia interfaz con el kit visual compartido <see cref="UiKit"/>
/// y conecta cada botón a una función real. El fondo vivo lo pone
/// <see cref="MainMenuBackdrop"/>; aquí solo hay acabado: velos, marco doble,
/// título con halo palpitante y tarjeta que flota muy despacio.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private GameObject optionsPanel, creditsPanel, mainPanel;
    private Text masterLabel, musicLabel, sfxLabel, difficultyLabel;

    private Outline titleHalo;
    private RectTransform cardRt;
    private float cardBaseY;
    private float pulse;

    private int difficultyLevel;

    void Awake()
    {
        GameBootstrap.EnsureCoreObjects();
        BuildUi();
    }

    void Start()
    {
        RefreshVolumeLabels();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic();
    }

    void Update()
    {
        // Halo del título: contorno dorado que respira, sin duplicar el texto.
        if (titleHalo != null)
        {
            pulse += Time.deltaTime * 1.0f;
            float t = (Mathf.Sin(pulse) + 1f) * 0.5f;

            Color c = titleHalo.effectColor;
            c.a = Mathf.Lerp(0.16f, 0.42f, t);
            titleHalo.effectColor = c;
        }

        // La tarjeta flota dos píxeles: quita la sensación de pantalla congelada.
        if (cardRt != null)
        {
            Vector2 p = cardRt.anchoredPosition;
            p.y = cardBaseY + Mathf.Sin(Time.time * 0.75f) * 3f;
            cardRt.anchoredPosition = p;
        }
    }

    // ---------- Construcción ----------

    void BuildUi()
    {
        Canvas canvas = UiKit.UiCanvas("MainMenuCanvas", transform, 100);
        Transform root = canvas.transform;

        // Velos: oscurecen arriba y abajo para que el texto se lea sobre la arboleda.
        UiKit.UiVeil("VeilTop", root, new Color(0f, 0f, 0f, 0.72f), true);
        UiKit.UiVeil("VeilBottom", root, new Color(0f, 0f, 0f, 0.80f), false);

        mainPanel = new GameObject("MainPanel", typeof(RectTransform));
        mainPanel.transform.SetParent(root, false);
        RectTransform mainRt = mainPanel.GetComponent<RectTransform>();
        mainRt.anchorMin = Vector2.zero;
        mainRt.anchorMax = Vector2.one;
        mainRt.offsetMin = Vector2.zero;
        mainRt.offsetMax = Vector2.zero;

        Text title = UiKit.UiText("Title", mainPanel.transform, "LA CORRUPCIÓN DE LA ARBOLEDA",
            66, TextAnchor.MiddleCenter, UiKit.GoldBright, true);
        UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -100f), new Vector2(1600f, 90f));

        titleHalo = title.gameObject.AddComponent<Outline>();
        titleHalo.effectColor = new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.30f);
        titleHalo.effectDistance = new Vector2(2f, -2f);
        titleHalo.useGraphicAlpha = false;

        // Filete dorado bajo el título.
        Image titleRule = UiKit.UiBackdrop("TitleRule", mainPanel.transform,
            new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.70f), false);
        UiKit.Place(titleRule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -152f), new Vector2(560f, 2f));

        Text subtitle = UiKit.UiText("Subtitle", mainPanel.transform,
            "Cuatro fragmentos del sello protegen el bosque.\n" +
            "La corrupción los ha dispersado entre el bosque, la aldea, las cuevas y las ruinas.\n" +
            "Recupéralos y derrota al Guardián del Santuario.",
            25, TextAnchor.MiddleCenter, UiKit.Parchment);
        UiKit.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -206f), new Vector2(1500f, 110f));

        // Sombra proyectada de la tarjeta.
        Image cardShadow = UiKit.UiBackdrop("MenuCardShadow", mainPanel.transform, new Color(0f, 0f, 0f, 0.55f), false);
        UiKit.Place(cardShadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -66f), new Vector2(668f, 458f));

        // Tarjeta central con los botones.
        RectTransform card = UiKit.UiPanel("MenuCard", mainPanel.transform,
            new Color(0.045f, 0.055f, 0.070f, 0.93f), UiKit.Gold, 3f);
        UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -70f), new Vector2(640f, 430f));
        cardRt = card;
        cardBaseY = card.anchoredPosition.y;

        // Segundo marco interior: le da el aire de "pergamino encuadernado".
        RectTransform innerFrame = UiKit.UiPanel("MenuCardInner", card,
            new Color(0f, 0f, 0f, 0f), new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.22f), 1f);
        UiKit.Stretch(innerFrame, 10f);

        Text cardTitle = UiKit.UiSection("MenuCardTitle", card, "Elige tu camino");
        UiKit.Place(cardTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -22f), new Vector2(400f, 24f));

        Image divider = UiKit.UiBackdrop("MenuDivider", card,
            new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.45f), false);
        UiKit.Place(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -56f), new Vector2(520f, 2f));

        Button play = UiKit.UiButton("PlayButton", card, "JUGAR", new Vector2(500f, 64f), 25,
            UiKit.Moss, UiKit.GoldBright, StartGame);
        UiKit.Place(play.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(500f, 64f));

        Button options = UiKit.UiButton("OptionsButton", card, "OPCIONES", new Vector2(500f, 56f), 22,
            UiKit.Steel, UiKit.Gold, OpenOptions);
        UiKit.Place(options.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -168f), new Vector2(500f, 56f));

        Button credits = UiKit.UiButton("CreditsButton", card, "CRÉDITOS", new Vector2(500f, 56f), 22,
            new Color(0.24f, 0.22f, 0.31f, 1f), UiKit.Gold, OpenCredits);
        UiKit.Place(credits.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(500f, 56f));

        Button quit = UiKit.UiButton("QuitButton", card, "SALIR", new Vector2(500f, 56f), 22,
            UiKit.Blood, UiKit.Gold, QuitGame);
        UiKit.Place(quit.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -296f), new Vector2(500f, 56f));

        Text cardHint = UiKit.UiText("MenuHint", card, "La partida se guarda al entrar en cada zona.",
            16, TextAnchor.MiddleCenter, UiKit.ParchmentDim);
        UiKit.Place(cardHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 32f), new Vector2(500f, 30f));

        Text controls = UiKit.UiText("Controls", mainPanel.transform,
            "WASD / Flechas: mover   ·   Espacio: cuerpo a cuerpo   ·   F o clic derecho: distancia   ·   Shift: esquiva\n" +
            "E: hablar con NPC   ·   C: comerciar   ·   I / Tab: mochila   ·   Esc: pausa",
            21, TextAnchor.MiddleCenter, UiKit.ParchmentDim);
        UiKit.Place(controls.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 46f), new Vector2(1500f, 70f));

        // ---------------- Opciones ----------------
        optionsPanel = UiKit.UiBackdrop("OptionsPanel", root, new Color(0.01f, 0.02f, 0.03f, 0.90f), true).gameObject;
        UiKit.UiVeil("OptionsVeil", optionsPanel.transform, new Color(0f, 0f, 0f, 0.55f), false);

        RectTransform optionsCard = UiKit.UiPanel("OptionsCard", optionsPanel.transform,
            new Color(0.045f, 0.055f, 0.070f, 0.97f), UiKit.Gold, 3f);
        UiKit.Place(optionsCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 620f));

        RectTransform optionsInner = UiKit.UiPanel("OptionsCardInner", optionsCard,
            new Color(0f, 0f, 0f, 0f), new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.22f), 1f);
        UiKit.Stretch(optionsInner, 10f);

        Text optionsTitle = UiKit.UiText("OptionsTitle", optionsCard, "OPCIONES", 40, TextAnchor.MiddleCenter, UiKit.GoldBright, true);
        UiKit.Place(optionsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -28f), new Vector2(700f, 50f));

        Image optionsRule = UiKit.UiBackdrop("OptionsRule", optionsCard,
            new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.45f), false);
        UiKit.Place(optionsRule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -66f), new Vector2(700f, 2f));

        masterLabel = CreateStepper(optionsCard, "Volumen general", 0, -110f);
        musicLabel = CreateStepper(optionsCard, "Música", 1, -196f);
        sfxLabel = CreateStepper(optionsCard, "Efectos", 2, -282f);

        difficultyLabel = UiKit.UiText("DifficultyLabel", optionsCard, "Dificultad: Normal", 24,
            TextAnchor.MiddleLeft, UiKit.GoldBright);
        UiKit.Place(difficultyLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(60f, -368f), new Vector2(420f, 40f));

        Button difficultyToggle = UiKit.UiButton("DifficultyToggle", optionsCard, "CAMBIAR",
            new Vector2(200f, 54f), 20, new Color(0.30f, 0.36f, 0.20f, 1f), UiKit.Gold, ToggleDifficulty);
        UiKit.Place(difficultyToggle.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-60f, -361f), new Vector2(200f, 54f));

        Button optionsBack = UiKit.UiButton("OptionsBack", optionsCard, "VOLVER", new Vector2(280f, 56f), 20,
            UiKit.Steel, UiKit.Gold, CloseOptions);
        UiKit.Place(optionsBack.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(280f, 56f));
        optionsPanel.SetActive(false);

        // ---------------- Créditos ----------------
        creditsPanel = UiKit.UiBackdrop("CreditsPanel", root, new Color(0.01f, 0.015f, 0.03f, 0.92f), true).gameObject;
        UiKit.UiVeil("CreditsVeil", creditsPanel.transform, new Color(0f, 0f, 0f, 0.55f), false);

        RectTransform creditsCard = UiKit.UiPanel("CreditsCard", creditsPanel.transform,
            new Color(0.045f, 0.055f, 0.070f, 0.97f), UiKit.Gold, 3f);
        UiKit.Place(creditsCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1100f, 760f));

        RectTransform creditsInner = UiKit.UiPanel("CreditsCardInner", creditsCard,
            new Color(0f, 0f, 0f, 0f), new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.22f), 1f);
        UiKit.Stretch(creditsInner, 10f);

        Text creditsTitle = UiKit.UiText("CreditsTitle", creditsCard, "CRÉDITOS", 40, TextAnchor.MiddleCenter, UiKit.GoldBright, true);
        UiKit.Place(creditsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -28f), new Vector2(700f, 50f));

        Image creditsRule = UiKit.UiBackdrop("CreditsRule", creditsCard,
            new Color(UiKit.Gold.r, UiKit.Gold.g, UiKit.Gold.b, 0.45f), false);
        UiKit.Place(creditsRule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -66f), new Vector2(900f, 2f));

        // Créditos finales reales, con una sola fuente de verdad: Ending.CreditsBody().
        // El área termina 40 px arriba del botón VOLVER, de modo que el texto nunca
        // puede quedar tapado por él aunque la tarjeta se estreche.
        Text creditsBody = UiKit.UiText("CreditsBody", creditsCard, Ending.CreditsBody(),
            22, TextAnchor.UpperCenter, UiKit.Parchment);
        creditsBody.resizeTextForBestFit = true;
        creditsBody.resizeTextMinSize = 12;
        creditsBody.resizeTextMaxSize = 22;
        UiKit.Place(creditsBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -100f), new Vector2(1000f, 530f));

        Button creditsBack = UiKit.UiButton("CreditsBack", creditsCard, "VOLVER", new Vector2(280f, 56f), 20,
            UiKit.Steel, UiKit.Gold, CloseCredits);
        UiKit.Place(creditsBack.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(280f, 56f));
        creditsPanel.SetActive(false);
    }

    Text CreateStepper(Transform parent, string title, int channel, float y)
    {
        Text label = UiKit.UiText(title + "Label", parent, title, 24, TextAnchor.MiddleLeft, UiKit.Parchment);
        UiKit.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(60f, y), new Vector2(520f, 40f));

        Button minus = UiKit.UiSmallButton(title + "Minus", parent, "−", UiKit.Steel, () => ChangeVolume(channel, -0.1f));
        UiKit.Place(minus.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-150f, y + 4f), new Vector2(62f, 50f));

        Button plus = UiKit.UiSmallButton(title + "Plus", parent, "+", UiKit.Steel, () => ChangeVolume(channel, 0.1f));
        UiKit.Place(plus.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-70f, y + 4f), new Vector2(62f, 50f));

        return label;
    }

    // ---------- Acciones ----------

    void OpenOptions()
    {
        optionsPanel.SetActive(true);
        RefreshVolumeLabels();
    }

    void CloseOptions() { optionsPanel.SetActive(false); }

    void OpenCredits() { creditsPanel.SetActive(true); }

    void CloseCredits() { creditsPanel.SetActive(false); }

    /// <summary>Abre la escena de créditos finales (marquesina + reflexión + equipo).</summary>
    void OpenCreditsScene()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            GameBootstrap.EnsureCoreObjects();
            gm = GameManager.Instance;
        }
        if (gm != null) gm.LoadCredits();
    }

    void ChangeVolume(int channel, float delta)
    {
        AudioManager am = AudioManager.Instance;
        if (am == null) return;
        if (channel == 0) am.SetMasterVolume(am.MasterVolume + delta);
        else if (channel == 1) am.SetMusicVolume(am.MusicVolume + delta);
        else am.SetSfxVolume(am.SfxVolume + delta);
        RefreshVolumeLabels();
        am.PlaySfx(SfxId.Item);
    }

    void ToggleDifficulty()
    {
        difficultyLevel = difficultyLevel == 0 ? 1 : 0;
        Difficulty.Set(difficultyLevel);
        if (difficultyLabel != null) difficultyLabel.text = "Dificultad: " + Difficulty.Name;
    }

    void RefreshVolumeLabels()
    {
        AudioManager am = AudioManager.Instance;
        if (am == null) return;
        if (masterLabel != null) masterLabel.text = "Volumen general: " + Mathf.RoundToInt(am.MasterVolume * 100f) + "%";
        if (musicLabel != null) musicLabel.text = "Música: " + Mathf.RoundToInt(am.MusicVolume * 100f) + "%";
        if (sfxLabel != null) sfxLabel.text = "Efectos: " + Mathf.RoundToInt(am.SfxVolume * 100f) + "%";
        if (difficultyLabel != null) difficultyLabel.text = "Dificultad: " + Difficulty.Name;
    }

    public void StartGame()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            GameBootstrap.EnsureCoreObjects();
            gm = GameManager.Instance;
        }
        if (gm != null) gm.StartNewGame();
    }

    public void QuitGame()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null) gm.QuitGame();
    }
}
