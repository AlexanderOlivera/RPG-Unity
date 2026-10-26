using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Sonda temporal: busca lineas rectas largas (cruces) en (a) un render propio de la camara
/// a RenderTexture y (b) todas las capturas de Aura, para distinguir artefacto del juego
/// de artefacto del pipeline de captura.
/// </summary>
public static class AuraFrameProbe
{
    const string Folder = @"C:\Users\leona\AppData\Local\AuraForUnity\CapturedImages\c19c4c86d0a94efe";

    public static string Run()
    {
        StringBuilder sb = new StringBuilder();

        // ---------- (a) Render propio de la camara, sin UI ----------
        Camera cam = Camera.main;
        if (cam == null) { sb.AppendLine("SIN CAMARA"); return sb.ToString(); }

        int w = Mathf.Max(2, cam.pixelWidth);
        int h = Mathf.Max(2, cam.pixelHeight);
        sb.AppendLine("=== RENDER PROPIO (camara sola, sin canvas) " + w + "x" + h + " ===");

        RenderTexture rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        RenderTexture prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = prev;

        RenderTexture.active = rt;
        Texture2D shot = new Texture2D(w, h, TextureFormat.RGBA32, false);
        shot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        shot.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        Analyse(sb, shot.GetPixels(), w, h, "render");

        // ---------- (b) Todas las capturas de Aura ----------
        sb.AppendLine();
        sb.AppendLine("=== CAPTURAS DE AURA ===");
        if (!Directory.Exists(Folder)) { sb.AppendLine("carpeta no encontrada"); return sb.ToString(); }

        string[] files = Directory.GetFiles(Folder, "*.jpeg");
        for (int i = 0; i < files.Length; i++)
        {
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!t.LoadImage(File.ReadAllBytes(files[i]))) { sb.AppendLine(Path.GetFileName(files[i]) + ": no decodifica"); continue; }
            sb.AppendLine();
            sb.AppendLine("--- " + Path.GetFileName(files[i]) + " ---");
            Analyse(sb, t.GetPixels(), t.width, t.height, "jpeg");
        }

        return sb.ToString();
    }

    static void Analyse(StringBuilder sb, Color[] px, int W, int H, string tag)
    {
        float[] lum = new float[W * H];
        for (int i = 0; i < lum.Length; i++)
            lum[i] = px[i].r * 0.299f + px[i].g * 0.587f + px[i].b * 0.114f;

        int bestCol = 0, bestColRun = 0;
        for (int x = 0; x < W; x++)
        {
            int run = LongestRun(lum, W, H, x, true, 0.12f);
            if (run > bestColRun) { bestColRun = run; bestCol = x; }
        }

        int bestRow = 0, bestRowRun = 0;
        for (int y = 0; y < H; y++)
        {
            int run = LongestRun(lum, W, H, y, false, 0.12f);
            if (run > bestRowRun) { bestRowRun = run; bestRow = y; }
        }

        sb.AppendLine(tag + " " + W + "x" + H +
                      " | MEJOR COLUMNA x=" + bestCol + " (" + (100f * bestCol / W).ToString("F1") + "%) run=" + bestColRun + "/" + H +
                      " | MEJOR FILA yDesdeAbajo=" + bestRow + " (" + (100f * bestRow / H).ToString("F1") + "%) run=" + bestRowRun + "/" + W);

        // Si hay una linea vertical clara, perfil de color alrededor
        if (bestColRun > H / 3)
        {
            StringBuilder p = new StringBuilder("   perfil fila " + (H / 2) + " alrededor de x=" + bestCol + ": ");
            for (int d = -6; d <= 6; d++)
            {
                int x = Mathf.Clamp(bestCol + d, 0, W - 1);
                int y = H / 2;
                p.Append("[" + d + "]" + (px[y * W + x].r * 255f).ToString("F0") + "," +
                         (px[y * W + x].g * 255f).ToString("F0") + "," + (px[y * W + x].b * 255f).ToString("F0") + " ");
            }
            sb.AppendLine(p.ToString());
        }

        if (bestRowRun > W / 3)
        {
            StringBuilder p = new StringBuilder("   perfil columna izq alrededor de y=" + bestRow + ": ");
            for (int d = -6; d <= 6; d++)
            {
                int y = Mathf.Clamp(bestRow + d, 0, H - 1);
                int x = W / 10;
                p.Append("[" + d + "]" + (px[y * W + x].r * 255f).ToString("F0") + "," +
                         (px[y * W + x].g * 255f).ToString("F0") + "," + (px[y * W + x].b * 255f).ToString("F0") + " ");
            }
            sb.AppendLine(p.ToString());
        }
    }

    static int LongestRun(float[] lum, int W, int H, int fixedCoord, bool vertical, float threshold)
    {
        int n = vertical ? H : W;
        int best = 0, run = 0;
        for (int i = 0; i < n; i++)
        {
            float v = vertical ? lum[i * W + fixedCoord] : lum[fixedCoord * W + i];
            if (v > threshold) { run++; if (run > best) best = run; }
            else run = 0;
        }
        return best;
    }
}
