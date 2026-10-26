using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Kit visual compartido por toda la interfaz del RPG (HUD, menús, diálogos).
/// Paleta de pergamino y oro sobre tinta oscura, marcos con borde, textos con
/// sombra y botones con estados de color para un acabado temático.
/// </summary>
public static class UiKit
{
    // ---- Paleta ----
    public static readonly Color Ink = new Color(0.055f, 0.065f, 0.085f, 0.94f);
    public static readonly Color InkSoft = new Color(0.085f, 0.10f, 0.13f, 0.88f);
    public static readonly Color Parchment = new Color(0.93f, 0.89f, 0.77f, 1f);
    public static readonly Color ParchmentDim = new Color(0.72f, 0.70f, 0.62f, 1f);
    public static readonly Color Gold = new Color(0.80f, 0.65f, 0.33f, 1f);
    public static readonly Color GoldBright = new Color(0.97f, 0.85f, 0.55f, 1f);
    public static readonly Color Moss = new Color(0.30f, 0.46f, 0.29f, 1f);
    public static readonly Color MossBright = new Color(0.55f, 0.80f, 0.48f, 1f);
    public static readonly Color Blood = new Color(0.42f, 0.12f, 0.13f, 1f);
    public static readonly Color BloodBright = new Color(0.85f, 0.32f, 0.32f, 1f);
    public static readonly Color Steel = new Color(0.24f, 0.31f, 0.40f, 0.96f);
    public static readonly Color SteelBright = new Color(0.40f, 0.52f, 0.66f, 1f);
    public static readonly Color Shade = new Color(0f, 0f, 0f, 0.55f);

    private static Font cachedFont;
    private static Sprite cachedVeil;

    public static Font Font
    {
        get
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return cachedFont;
        }
    }

    // ---- Canvas ----
    public static Canvas UiCanvas(string name, Transform parent, int sortingOrder)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    // ---- Anclajes ----
    public static RectTransform Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                      Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        return rt;
    }

    public static RectTransform Stretch(RectTransform rt, float padding = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
        return rt;
    }

    // ---- Marcos y fondos ----
    /// <summary>Panel con relleno y borde. Devuelve el RectTransform exterior.</summary>
    public static RectTransform UiPanel(string name, Transform parent, Color fill, Color border, float borderWidth)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        Image bg = go.GetComponent<Image>();
        bg.color = border;
        bg.raycastTarget = false;

        GameObject inner = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(go.transform, false);
        Image fillImage = inner.GetComponent<Image>();
        fillImage.color = fill;
        fillImage.raycastTarget = false;

        Stretch(inner.GetComponent<RectTransform>(), borderWidth);
        return go.GetComponent<RectTransform>();
    }

    /// <summary>Fondo a pantalla completa (para atenuar y bloquear clics).</summary>
    public static Image UiBackdrop(string name, Transform parent, Color color, bool blocksClicks)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = blocksClicks;
        Stretch(go.GetComponent<RectTransform>());
        return img;
    }

    // ---- Velos y degradados ----
    /// <summary>Sprite de degradado blanco arriba → transparente abajo, cacheado.</summary>
    private static Sprite VeilSprite()
    {
        if (cachedVeil != null) return cachedVeil;

        const int height = 128;
        Texture2D tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            tex.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.Pow(1f - t, 1.6f)));
        }

        tex.Apply();
        cachedVeil = Sprite.Create(tex, new Rect(0f, 0f, 1f, height), new Vector2(0.5f, 0.5f), 1f);
        return cachedVeil;
    }

    /// <summary>Velo degradado a pantalla completa (viñeta superior, inferior o lateral girada).</summary>
    public static Image UiVeil(string name, Transform parent, Color color, bool fromTop, bool blocksClicks = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        Image img = go.GetComponent<Image>();
        img.sprite = VeilSprite();
        img.color = color;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.raycastTarget = blocksClicks;

        if (!fromTop) go.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        Stretch(go.GetComponent<RectTransform>());
        return img;
    }

    // ---- Textos ----
    public static Text UiText(string name, Transform parent, string content, int size, TextAnchor anchor,
                              Color color, bool bold = false, bool withShadow = true)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.font = Font;
        text.text = content;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = true;

        if (withShadow)
        {
            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = Shade;
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }
        return text;
    }

    /// <summary>Etiqueta de sección en versalitas doradas.</summary>
    public static Text UiSection(string name, Transform parent, string content)
    {
        return UiText(name, parent, content.ToUpperInvariant(), 17, TextAnchor.MiddleLeft, Gold, true);
    }

    /// <summary>Línea decorativa dorada sujeta al borde superior del padre.</summary>
    public static Image UiRule(string name, Transform parent, float height, float inset)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = Gold;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(inset, -height);
        rt.offsetMax = new Vector2(-inset, 0f);
        return img;
    }

    // ---- Botones ----
    public static Button UiButton(string name, Transform parent, string label, Vector2 size, int fontSize,
                                  Color fill, Color border, UnityAction onClick)
    {
        RectTransform frame = UiPanel(name, parent, fill, border, 2f);
        frame.sizeDelta = size;

        GameObject clickable = frame.gameObject;
        Button button = clickable.AddComponent<Button>();

        // UiPanel desactiva raycastTarget en sus dos imágenes (para que los paneles
        // decorativos no bloqueen clics). Un botón necesita justo lo contrario: sin
        // un Graphic con raycastTarget el GraphicRaycaster lo ignora y el botón
        // nunca recibe el clic. Por eso se reactiva aquí, solo en los botones.
        Transform inner = frame.Find("Fill");
        Image frameImage = clickable.GetComponent<Image>();
        Image innerImage = inner != null ? inner.GetComponent<Image>() : null;
        if (frameImage != null) frameImage.raycastTarget = true;
        if (innerImage != null) innerImage.raycastTarget = true;
        button.targetGraphic = innerImage != null ? innerImage : frameImage;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.18f, 1.18f, 1.10f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        if (onClick != null) button.onClick.AddListener(onClick);

        Text text = UiText(name + "_Label", frame, label, fontSize, TextAnchor.MiddleCenter, GoldBright, true);
        Stretch(text.rectTransform, 4f);
        return button;
    }

    public static Button UiSmallButton(string name, Transform parent, string label, Color fill, UnityAction onClick)
    {
        return UiButton(name, parent, label, new Vector2(62f, 50f), 26, fill, Gold, onClick);
    }

    // ---- Barra (vida, etc.) ----
    public static Image UiBar(string name, Transform parent, Vector2 size, Color background, Color fill,
                              out Text overlay, string overlayText)
    {
        RectTransform frame = UiPanel(name, parent, background, Gold, 2f);
        frame.sizeDelta = size;

        Transform inner = frame.Find("Fill");
        Image fillImage = null;
        if (inner != null)
        {
            fillImage = inner.GetComponent<Image>();
            fillImage.color = fill;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 1f;
        }

        overlay = UiText(name + "_Value", frame, overlayText, 18, TextAnchor.MiddleCenter, Parchment, true);
        Stretch(overlay.rectTransform, 2f);
        return fillImage;
    }

    /// <summary>Chip pequeño para una estadística del HUD.</summary>
    public static Text UiChip(string name, Transform parent, Vector2 size, Vector2 pos, Color valueColor)
    {
        RectTransform frame = UiPanel(name, parent, InkSoft, new Color(Gold.r, Gold.g, Gold.b, 0.45f), 1f);
        Place(frame, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);

        Text label = UiText(name + "_Value", frame, string.Empty, 17, TextAnchor.MiddleLeft, valueColor, true);
        Stretch(label.rectTransform, 8f);
        return label;
    }
}
