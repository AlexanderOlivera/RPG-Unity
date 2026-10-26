using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sonda temporal: renderiza la camara a una RenderTexture incluyendo el canvas
/// (pasandolo a ScreenSpaceCamera) y compara variantes para identificar la cruz
/// fina que aparece en el centro de la pantalla.
/// </summary>
public static class AuraMenuVerdict
{
    const float Threshold = 0.12f;

    public static string Run()
    {
        StringBuilder sb = new StringBuilder();

        Camera cam = Camera.main;
        if (cam == null) return "SIN CAMARA";

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas canvas = canvases.Length > 0 ? canvases[0] : null;

        int w = Mathf.Max(2, cam.pixelWidth);
        int h = Mathf.Max(2, cam.pixelHeight);
        sb.AppendLine("Resolucion de render=" + w + "x" + h + "  canvas=" + (canvas != null ? canvas.name : "ninguno"));

        // --- A) Solo mundo ---
        Analyse(sb, Grab(cam, w, h), w, h, "A) mundo solo (sin canvas)");

        if (canvas == null) return sb.ToString();

        RenderMode prevMode = canvas.renderMode;
        Camera prevCam = canvas.worldCamera;
        float prevPlane = canvas.planeDistance;

        // Pasar el overlay delante de la camara para que entre en su render.
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 5f;
        Canvas.ForceUpdateCanvases();

        // --- B) Mundo + UI ---
        Analyse(sb, Grab(cam, w, h), w, h, "B) mundo + UI completa");

        // --- C) Sin velos ---
        GameObject veilTop = Find("VeilTop");
        GameObject veilBottom = Find("VeilBottom");
        bool t = veilTop != null && veilTop.activeSelf;
        bool b = veilBottom != null && veilBottom.activeSelf;
        if (veilTop != null) veilTop.SetActive(false);
        if (veilBottom != null) veilBottom.SetActive(false);
        Canvas.ForceUpdateCanvases();
        Analyse(sb, Grab(cam, w, h), w, h, "C) mundo + UI SIN velos");
        if (veilTop != null) veilTop.SetActive(t);
        if (veilBottom != null) veilBottom.SetActive(b);

        // --- D) Sin el canvas entero ---
        canvas.enabled = false;
        Canvas.ForceUpdateCanvases();
        Analyse(sb, Grab(cam, w, h), w, h, "D) canvas desactivado");
        canvas.enabled = true;

        // --- E) Sin el fondo vivo ---
        GameObject backdrop = Find("MenuBackdrop");
        if (backdrop != null)
        {
            backdrop.SetActive(false);
            Canvas.ForceUpdateCanvases();
            Analyse(sb, Grab(cam, w, h), w, h, "E) sin MenuBackdrop");
            backdrop.SetActive(true);
        }

        canvas.renderMode = prevMode;
        canvas.worldCamera = prevCam;
        canvas.planeDistance = prevPlane;
        Canvas.ForceUpdateCanvases();

        return sb.ToString();
    }

    static GameObject Find(string name)
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++) if (all[i].name == name) return all[i];
        return null;
    }

    static Color[] Grab(Camera cam, int w, int h)
    {
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        RenderTexture prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = prev;

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        Color[] px = tex.GetPixels();
        Object.DestroyImmediate(tex);
        return px;
    }

    static void Analyse(StringBuilder sb, Color[] px, int W, int H, string label)
    {
        float[] lum = new float[W * H];
        for (int i = 0; i < lum.Length; i++)
            lum[i] = px[i].r * 0.299f + px[i].g * 0.587f + px[i].b * 0.114f;

        int bestCol = 0, bestColRun = 0;
        for (int x = 0; x < W; x++)
        {
            int run = LongestRun(lum, W, H, x, true);
            if (run > bestColRun) { bestColRun = run; bestCol = x; }
        }
        int bestRow = 0, bestRowRun = 0;
        for (int y = 0; y < H; y++)
        {
            int run = LongestRun(lum, W, H, y, false);
            if (run > bestRowRun) { bestRowRun = run; bestRow = y; }
        }

        sb.AppendLine(label + " -> colRun=" + bestColRun + "/" + H + " en x=" + bestCol +
                      " (" + (100f * bestCol / W).ToString("F1") + "%); rowRun=" + bestRowRun + "/" + W +
                      " en yDesdeAbajo=" + bestRow + " (" + (100f * bestRow / H).ToString("F1") + "%)");

        if (bestColRun > H / 3 || bestRowRun > W / 3)
            sb.AppendLine("    !! LINEA LARGA DETECTADA (posible cruz)");

        // Muestra el color del centro y de sus vecinos, y el color del borde
        int cx = W / 2, cy = H / 2;
        sb.AppendLine("    centro=" + Rgb(px[cy * W + cx]) +
                      " izq=" + Rgb(px[cy * W + Mathf.Max(0, cx - 8)]) +
                      " der=" + Rgb(px[cy * W + Mathf.Min(W - 1, cx + 8)]) +
                      " arriba=" + Rgb(px[Mathf.Min(H - 1, cy + 8) * W + cx]) +
                      " abajo=" + Rgb(px[Mathf.Max(0, cy - 8) * W + cx]));
    }

    static string Rgb(Color c)
    {
        return "(" + (c.r * 255f).ToString("F0") + "," + (c.g * 255f).ToString("F0") + "," + (c.b * 255f).ToString("F0") + ")";
    }

    static int LongestRun(float[] lum, int W, int H, int fixedCoord, bool vertical)
    {
        int n = vertical ? H : W;
        int best = 0, run = 0;
        for (int i = 0; i < n; i++)
        {
            float v = vertical ? lum[i * W + fixedCoord] : lum[fixedCoord * W + i];
            if (v > Threshold) { run++; if (run > best) best = run; }
            else run = 0;
        }
        return best;
    }
}
