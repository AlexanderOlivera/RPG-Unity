using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fondo vivo del menú principal: cielo degradado, luna con halo, tres capas de
/// arboleda en parallax, suelo, niebla arrastrada, matas en primer plano y
/// luciérnagas. Es puramente estético: no toca ninguna función del menú.
/// Todo se genera en Awake a partir de los sprites que asigna el generador de escenas.
/// </summary>
public class MainMenuBackdrop : MonoBehaviour
{
    [Header("Siluetas (las asigna GameSceneBuilder)")]
    public Sprite[] treeSprites;
    public Sprite[] bushSprites;

    [Header("Velocidad de parallax (unidades por segundo)")]
    public float farSpeed = 0.045f;
    public float midSpeed = 0.11f;
    public float nearSpeed = 0.22f;

    [Header("Ambiente")]
    public int fireflyCount = 26;

    class Layer
    {
        public Transform tr;
        public float speed;
        public float width;
    }

    readonly List<Layer> layers = new List<Layer>();

    SpriteRenderer moonHalo;
    SpriteRenderer moonCore;
    Vector3 moonHaloBaseScale;
    Vector3 moonCoreBaseScale;
    float moonPhase;

    float halfWidth = 9f;
    float halfHeight = 5f;

    void Awake()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            halfHeight = cam.orthographicSize;
            halfWidth = cam.orthographicSize * Mathf.Max(1f, cam.aspect);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.016f, 0.030f, 0.026f);
        }

        BuildSky();
        BuildMoon();

        BuildTreeline("Arboleda_Lontananza", treeSprites, 2.6f, -1.0f,
            new Color(0.11f, 0.17f, 0.15f, 1f), -52, farSpeed, 1.15f);
        BuildTreeline("Arboleda_Media", treeSprites, 3.9f, -2.0f,
            new Color(0.055f, 0.095f, 0.080f, 1f), -44, midSpeed, 0.95f);
        BuildMist();
        BuildTreeline("Arboleda_Cercana", treeSprites, 5.6f, -3.2f,
            new Color(0.024f, 0.044f, 0.038f, 1f), -36, nearSpeed, 0.8f);

        BuildGround();
        BuildBushes();
        BuildFireflies();
    }

    void Update()
    {
        for (int i = 0; i < layers.Count; i++)
        {
            Layer l = layers[i];
            if (l.tr == null) continue;

            Vector3 p = l.tr.position;
            p.x -= l.speed * Time.deltaTime;
            if (p.x <= -l.width) p.x += l.width;
            l.tr.position = p;
        }

        // La luna respira muy despacio: es lo que hace que el menú no esté muerto.
        if (moonHalo != null)
        {
            moonPhase += Time.deltaTime * 0.32f;
            float t = (Mathf.Sin(moonPhase) + 1f) * 0.5f;

            Color c = moonHalo.color;
            c.a = Mathf.Lerp(0.14f, 0.28f, t);
            moonHalo.color = c;
            moonHalo.transform.localScale = moonHaloBaseScale * Mathf.Lerp(0.96f, 1.06f, t);

            if (moonCore != null)
            {
                Color cc = moonCore.color;
                cc.a = Mathf.Lerp(0.80f, 1f, t);
                moonCore.color = cc;
                moonCore.transform.localScale = moonCoreBaseScale * Mathf.Lerp(0.99f, 1.03f, t);
            }
        }
    }

    // ---------------- Construcción ----------------

    void BuildSky()
    {
        GameObject go = new GameObject("Cielo");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(0f, 0.6f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = VerticalSprite(96, new Color(0.020f, 0.052f, 0.046f), new Color(0.004f, 0.012f, 0.014f));
        sr.sortingLayerName = "fondo";
        sr.sortingOrder = -60;

        Fit(go.transform, sr, halfWidth * 2.4f, halfHeight * 2.6f);
    }

    void BuildMoon()
    {
        Sprite halo = RadialSprite(96, new Color(0.72f, 0.95f, 0.80f, 1f), 2.4f);
        Sprite core = RadialSprite(64, new Color(0.97f, 0.99f, 0.88f, 1f), 0.5f);

        GameObject haloGo = NewSprite("Luna_Halo", halo, new Color(0.72f, 0.95f, 0.80f, 0.20f), -56);
        haloGo.transform.position = new Vector3(halfWidth * 0.52f, halfHeight * 0.55f, 0f);
        moonHalo = haloGo.GetComponent<SpriteRenderer>();
        Fit(haloGo.transform, moonHalo, 7.2f, 7.2f);
        moonHaloBaseScale = haloGo.transform.localScale;

        GameObject coreGo = NewSprite("Luna_Disco", core, new Color(0.97f, 0.99f, 0.88f, 0.95f), -55);
        coreGo.transform.position = new Vector3(halfWidth * 0.52f, halfHeight * 0.55f, 0f);
        moonCore = coreGo.GetComponent<SpriteRenderer>();
        Fit(coreGo.transform, moonCore, 1.15f, 1.15f);
        moonCoreBaseScale = coreGo.transform.localScale;
    }

    void BuildTreeline(string name, Sprite[] sprites, float targetHeight, float y,
                       Color tint, int order, float speed, float gapFactor)
    {
        if (sprites == null || sprites.Length == 0) return;

        float rowWidth = halfWidth * 2f + 6f;

        GameObject layer = new GameObject(name);
        layer.transform.SetParent(transform, false);
        layer.transform.position = new Vector3(0f, y, 0f);

        // Dos tiras idénticas: al desplazarse, la segunda entra por la derecha sin costura.
        for (int copy = 0; copy < 2; copy++)
        {
            GameObject strip = new GameObject("Tira_" + copy);
            strip.transform.SetParent(layer.transform, false);
            strip.transform.localPosition = new Vector3(copy * rowWidth, 0f, 0f);

            float x = -rowWidth * 0.5f;
            while (x < rowWidth * 0.5f)
            {
                Sprite sprite = sprites[Random.Range(0, sprites.Length)];
                float spriteHeight = sprite != null ? sprite.bounds.size.y : 0f;
                if (spriteHeight <= 0.0001f) { x += 1.5f; continue; }

                float s = targetHeight * Random.Range(0.84f, 1.16f) / spriteHeight;

                GameObject tree = NewSprite("Arbol", sprite, tint, order, strip.transform);
                tree.transform.localPosition = new Vector3(x, Random.Range(-0.22f, 0.22f), 0f);
                tree.transform.localScale = new Vector3(s * (Random.value < 0.5f ? -1f : 1f), s, 1f);

                x += targetHeight * gapFactor * Random.Range(0.40f, 0.78f);
            }
        }

        layers.Add(new Layer { tr = layer.transform, speed = speed, width = rowWidth });
    }

    void BuildMist()
    {
        Sprite soft = RadialSprite(64, Color.white, 1.5f);
        float rowWidth = halfWidth * 2f + 8f;

        for (int i = 0; i < 2; i++)
        {
            GameObject layer = new GameObject("Niebla_" + i);
            layer.transform.SetParent(transform, false);
            layer.transform.position = new Vector3(0f, -1.2f - i * 1.35f, 0f);

            for (int copy = 0; copy < 2; copy++)
            {
                GameObject band = NewSprite("Velo", soft,
                    new Color(0.58f, 0.78f, 0.70f, 0.05f + i * 0.025f), -46 - i, layer.transform);
                band.transform.localPosition = new Vector3(copy * rowWidth, 0f, 0f);
                Fit(band.transform, band.GetComponent<SpriteRenderer>(), rowWidth, 1.5f);
            }

            layers.Add(new Layer { tr = layer.transform, speed = 0.03f + i * 0.02f, width = rowWidth });
        }
    }

    void BuildGround()
    {
        GameObject go = NewSprite("Suelo",
            VerticalSprite(16, new Color(0.010f, 0.022f, 0.019f), new Color(0.020f, 0.040f, 0.034f)),
            Color.white, -30);
        go.transform.position = new Vector3(0f, -halfHeight - 0.4f, 0f);
        Fit(go.transform, go.GetComponent<SpriteRenderer>(), halfWidth * 2.4f, 3.2f);
    }

    void BuildBushes()
    {
        if (bushSprites == null || bushSprites.Length == 0) return;

        GameObject root = new GameObject("Matas");
        root.transform.SetParent(transform, false);
        root.transform.position = new Vector3(0f, -halfHeight + 0.35f, 0f);

        float x = -halfWidth - 1f;
        while (x < halfWidth + 1f)
        {
            Sprite sprite = bushSprites[Random.Range(0, bushSprites.Length)];
            float spriteHeight = sprite != null ? sprite.bounds.size.y : 0f;
            if (spriteHeight <= 0.0001f) { x += 1f; continue; }

            float target = Random.Range(1.1f, 1.9f);
            float s = target / spriteHeight;

            GameObject bush = NewSprite("Mata", sprite, new Color(0.018f, 0.034f, 0.030f, 1f), -24, root.transform);
            bush.transform.localPosition = new Vector3(x, Random.Range(-0.15f, 0.15f), 0f);
            bush.transform.localScale = new Vector3(s * (Random.value < 0.5f ? -1f : 1f), s, 1f);

            x += Random.Range(0.9f, 1.8f);
        }
    }

    void BuildFireflies()
    {
        GameObject root = new GameObject("Luciernagas");
        root.transform.SetParent(transform, false);

        Sprite dot = RadialSprite(24, new Color(1f, 0.95f, 0.62f, 1f), 0.85f);

        for (int i = 0; i < fireflyCount; i++)
        {
            GameObject go = NewSprite("Luciernaga_" + i, dot, new Color(1f, 0.95f, 0.62f, 0.85f), -20, root.transform);
            go.transform.position = new Vector3(Random.Range(-halfWidth, halfWidth),
                                                Random.Range(-halfHeight * 0.75f, halfHeight * 0.8f), 0f);

            // El sprite mide 24 unidades: se escala al diámetro real de una luciérnaga.
            float diameter = Random.Range(0.10f, 0.22f);
            Fit(go.transform, go.GetComponent<SpriteRenderer>(), diameter, diameter);

            Firefly fly = go.AddComponent<Firefly>();
            fly.radius = Random.Range(0.5f, 1.7f);
            fly.speed = Random.Range(0.25f, 0.65f);
            fly.heightFactor = 0.5f;
            fly.minAlpha = 0.10f;
            fly.maxAlpha = 0.85f;
        }
    }

    // ---------------- Utilidades ----------------

    GameObject NewSprite(string name, Sprite sprite, Color tint, int order, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : transform, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tint;
        sr.sortingLayerName = "fondo";
        sr.sortingOrder = order;
        return go;
    }

    /// <summary>Escala el objeto para que el sprite mida width × height unidades.</summary>
    static void Fit(Transform t, SpriteRenderer sr, float width, float height)
    {
        if (sr == null || sr.sprite == null) return;
        Vector2 size = sr.sprite.bounds.size;
        if (size.x <= 0.0001f || size.y <= 0.0001f) return;
        t.localScale = new Vector3(width / size.x, height / size.y, 1f);
    }

    /// <summary>Degradado vertical de 4 px de ancho, 1 unidad = 1 píxel.</summary>
    static Sprite VerticalSprite(int height, Color bottom, Color top)
    {
        const int w = 4;
        Texture2D tex = new Texture2D(w, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            Color c = Color.Lerp(bottom, top, y / (float)(height - 1));
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, w, height), new Vector2(0.5f, 0.5f), 1f);
    }

    /// <summary>Mancha radial suave: sirve de halo, de niebla o de luciérnaga.</summary>
    static Sprite RadialSprite(int size, Color core, float power)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float maxRadius = size * 0.5f;
        Vector2 center = new Vector2(maxRadius, maxRadius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxRadius;
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), power);
                tex.SetPixel(x, y, new Color(core.r, core.g, core.b, core.a * a));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 1f);
    }
}
