using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Créditos finales: marquesina que asciende sobre un cielo nocturno con
/// luciérnagas, con la reflexión del equipo y los datos de la materia.
/// Se puede dejar correr o saltar con Esc / Espacio.
/// </summary>
public class CreditsUI : MonoBehaviour
{
    private RectTransform scrollRect;
    private Image[] motes;
    private Vector2[] moteOrigin;
    private float[] motePhase;
    private GameObject outroPanel;
    private bool finished;
    private float speed = 76f;
    private float scrollLength;

    void Awake()
    {
        Time.timeScale = 1f;
        GameBootstrap.EnsureCoreObjects();
        BuildUi();
    }

    void Update()
    {
        if (finished) return;

        float dt = Time.unscaledDeltaTime;
        AnimateMotes(dt);

        if (scrollRect != null)
        {
            scrollRect.anchoredPosition += Vector2.up * speed * dt;
            if (scrollRect.anchoredPosition.y >= 1180f) ShowOutro();
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return)) ShowOutro();
    }

    // ---------- Construcción ----------

    void BuildUi()
    {
        Canvas canvas = UiKit.UiCanvas("CreditsCanvas", transform, 100);
        Transform root = canvas.transform;

        UiKit.UiBackdrop("Night", root, new Color(0.012f, 0.022f, 0.030f, 1f), false);
        UiKit.UiBackdrop("Aurora", root, new Color(0.05f, 0.13f, 0.10f, 0.85f), false);

        BuildMotes(root);

        // Marco superior e inferior para dar sensación de cine.
        Image top = UiKit.UiBackdrop("TopBar", root, new Color(0f, 0f, 0f, 0.75f), false);
        top.rectTransform.anchorMin = new Vector2(0f, 0.90f);
        top.rectTransform.anchorMax = new Vector2(1f, 1f);
        top.rectTransform.offsetMin = Vector2.zero;
        top.rectTransform.offsetMax = Vector2.zero;

        Image bottom = UiKit.UiBackdrop("BottomBar", root, new Color(0f, 0f, 0f, 0.75f), false);
        bottom.rectTransform.anchorMin = new Vector2(0f, 0f);
        bottom.rectTransform.anchorMax = new Vector2(1f, 0.10f);
        bottom.rectTransform.offsetMin = Vector2.zero;
        bottom.rectTransform.offsetMax = Vector2.zero;

        Text header = UiKit.UiText("Header", root, "LA CORRUPCIÓN DE LA ARBOLEDA  ·  CRÉDITOS",
            22, TextAnchor.MiddleCenter, UiKit.GoldBright, true);
        UiKit.Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -32f), new Vector2(1400f, 40f));

        Button skip = UiKit.UiButton("SkipButton", root, "SALTAR  [Esc]", new Vector2(240f, 52f), 19,
            UiKit.Steel, UiKit.Gold, ShowOutro);
        UiKit.Place(skip.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-32f, 28f), new Vector2(240f, 52f));

        Text scrollHint = UiKit.UiText("ScrollHint", root, "Los créditos avanzan solos. Esc o Espacio para ver el cierre.",
            18, TextAnchor.MiddleLeft, UiKit.ParchmentDim);
        UiKit.Place(scrollHint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(36f, 36f), new Vector2(900f, 36f));

        // Visor con máscara: lo que sube solo se ve dentro de esta zona.
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(root, false);
        RectTransform viewportRt = viewport.GetComponent<RectTransform>();
        viewportRt.anchorMin = new Vector2(0f, 0.10f);
        viewportRt.anchorMax = new Vector2(1f, 0.90f);
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;

        GameObject scroll = new GameObject("Scroll", typeof(RectTransform));
        scroll.transform.SetParent(viewport.transform, false);
        scrollRect = scroll.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 0f);
        scrollRect.anchorMax = new Vector2(0.5f, 0f);
        scrollRect.pivot = new Vector2(0.5f, 0f);
        scrollRect.sizeDelta = new Vector2(1500f, 2400f);

        Text body = UiKit.UiText("Body", scroll.transform, ComposeText(), 22, TextAnchor.UpperCenter, UiKit.Parchment);
        UiKit.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(1500f, 2400f));

        // Ajustamos el alto real del texto para saber cuándo termina la marquesina.
        float height = Mathf.Max(600f, body.preferredHeight + 80f);
        body.rectTransform.sizeDelta = new Vector2(1500f, height);
        body.rectTransform.anchoredPosition = Vector2.zero;
        scrollRect.sizeDelta = new Vector2(1500f, height);
        scrollRect.anchoredPosition = new Vector2(0f, -height);
        scrollLength = height;

        BuildOutro(root);
    }

    string ComposeText()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.Append("<size=58><color=#F7D98C>LA CORRUPCIÓN DE LA ARBOLEDA</color></size>\n");
        sb.Append("<size=26>Un viaje por el bosque, la aldea, las cuevas y las ruinas</size>\n\n\n");

        if (GameSession.Score > 0 || GameSession.BossDefeated)
        {
            sb.Append("<size=26><color=#C9A85B>TU PARTIDA</color></size>\n");
            sb.Append("<size=22>").Append(Ending.Stats()).Append("</size>\n");
            sb.Append("<size=30><color=#F7D98C>Final: ").Append(Ending.Rank()).Append("</color></size>\n\n\n");
        }

        sb.Append("<size=40><color=#C9A85B>").Append("REFLEXIÓN").Append("</color></size>\n\n");
        sb.Append(StripFirstLine(Ending.Reflection())).Append("\n\n\n");

        sb.Append("<size=40><color=#C9A85B>EQUIPO E INSTITUCIÓN</color></size>\n\n");
        sb.Append(Ending.CreditsBody()).Append("\n\n\n");

        sb.Append("<size=20><color=#B9B5A6>Este juego fue creado como proyecto escolar y creció hasta ser un mundo ");
        sb.Append("que se puede recorrer, comerciar y salvar.</color></size>\n");

        return sb.ToString();
    }

    /// <summary>Quita el encabezado "REFLEXIÓN" del texto porque se titula aparte.</summary>
    static string StripFirstLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        int index = text.IndexOf('\n');
        return index >= 0 ? text.Substring(index + 1).TrimStart() : text;
    }

    void BuildMotes(Transform root)
    {
        const int count = 42;
        motes = new Image[count];
        moteOrigin = new Vector2[count];
        motePhase = new float[count];
        Sprite star = UiIcons.Get("star");

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Mote_" + i, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            Image img = go.GetComponent<Image>();
            img.sprite = star;
            img.raycastTarget = false;

            float x = Random.Range(0.03f, 0.97f);
            float y = Random.Range(0.05f, 0.95f);
            float size = Random.Range(8f, 20f);
            img.color = new Color(1f, 0.93f, 0.62f, Random.Range(0.10f, 0.45f));

            RectTransform rt = go.GetComponent<RectTransform>();
            UiKit.Place(rt, new Vector2(x, y), new Vector2(x, y), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(size, size));

            motes[i] = img;
            moteOrigin[i] = rt.anchorMin;
            motePhase[i] = Random.value * 10f;
        }
    }

    void AnimateMotes(float deltaTime)
    {
        if (motes == null) return;
        for (int i = 0; i < motes.Length; i++)
        {
            Image mote = motes[i];
            if (mote == null) continue;

            RectTransform rt = mote.rectTransform;
            Vector2 anchor = rt.anchorMin;
            anchor.y += 0.012f * deltaTime;
            if (anchor.y > 0.97f) anchor.y = 0.03f;
            anchor.x = Mathf.Clamp01(moteOrigin[i].x + Mathf.Sin(Time.unscaledTime * 0.35f + motePhase[i]) * 0.012f);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;

            Color c = mote.color;
            c.a = 0.12f + 0.30f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.2f + motePhase[i]));
            mote.color = c;
        }
    }

    void BuildOutro(Transform root)
    {
        outroPanel = UiKit.UiBackdrop("OutroPanel", root, new Color(0.01f, 0.02f, 0.03f, 0.93f), true).gameObject;

        RectTransform card = UiKit.UiPanel("OutroCard", outroPanel.transform, UiKit.Ink, UiKit.Gold, 3f);
        UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 560f));

        Text title = UiKit.UiText("OutroTitle", card, "GRACIAS POR JUGAR", 46, TextAnchor.MiddleCenter, UiKit.GoldBright, true);
        UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -34f), new Vector2(800f, 58f));
        UiKit.UiRule("OutroRule", card, 2f, 70f);

        string subtitle =
            "<size=24>RESENDIZ OLIVERA ALEXANDER JOSUE\n" +
            "ROSALES SANCHEZ ERIC LEONARDO</size>\n\n" +
            "<size=20><color=#C9A85B>FACULTAD DE ESTUDIOS SUPERIORES ARAGÓN · UNAM\n" +
            "MATERIA DE PROGRAMACIÓN DE VIDEOJUEGOS</color></size>\n\n" +
            "<size=19><color=#B9B5A6>«Custodia el sello con quienes caminan contigo.»</color></size>";

        Text body = UiKit.UiText("OutroBody", card, subtitle, 22, TextAnchor.UpperCenter, UiKit.Parchment);
        UiKit.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -110f), new Vector2(780f, 300f));

        Button replay = UiKit.UiButton("ReplayButton", card, "JUGAR DE NUEVO", new Vector2(360f, 58f), 21,
            UiKit.Moss, UiKit.Gold, Replay);
        UiKit.Place(replay.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(360f, 58f));

        Button menu = UiKit.UiButton("MenuButton", card, "MENÚ PRINCIPAL", new Vector2(360f, 58f), 21,
            UiKit.Steel, UiKit.Gold, BackToMenu);
        UiKit.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(360f, 58f));

        outroPanel.SetActive(false);
    }

    // ---------- Acciones ----------

    void ShowOutro()
    {
        if (finished) return;
        finished = true;
        if (outroPanel != null) outroPanel.SetActive(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(SfxId.Item);
    }

    void Replay()
    {
        if (GameManager.Instance != null) GameManager.Instance.StartNewGame();
    }

    void BackToMenu()
    {
        if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
    }
}
