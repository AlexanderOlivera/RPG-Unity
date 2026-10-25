using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Sonda temporal: captura el Game View con la API del motor (ScreenCapture) y analiza
/// el PNG resultante buscando lineas rectas largas. Sirve para distinguir un artefacto
/// real del juego de uno introducido por el pipeline de captura externo.
/// </summary>
public static class AuraShotTest
{
    const string Png = @"C:\Users\leona\OneDrive\Desktop\VIDEOJUEGOS\RPG-Unity\Temp_\aura_native.png";
    const float Threshold = 0.12f;

    public static string Capture()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Png));
        if (File.Exists(Png)) File.Delete(Png);
        ScreenCapture.CaptureScreenshot(Png);
        return "captura solicitada -> " + Png + " (Screen=" + Screen.width + "x" + Screen.height + ")";
    }

    public static string Report()
    {
        StringBuilder sb = new StringBuilder();
        if (!File.Exists(Png)) return "no existe el PNG; ejecuta Capture primero";

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(Png))) return "no decodifica";
        tex.wrapMode = TextureWrapMode.Clamp;

        int W = tex.width;
        int H = tex.height;
        sb.AppendLine("PNG propio (ScreenCapture del motor) = " + W + "x" + H);

        Color[] px = tex.GetPixels();
        float[] lum = new float[W * H];
        for (int i = 0; i < lum.Length; i++)
            lum[i] = px[i].r * 0.299f + px[i].g * 0.587f + px[i].b * 0.114f;

        sb.AppendLine("--- 6 tiradas verticales mas largas (columnas) ---");
        ReportRuns(sb, lum, px, W, H, true);
        sb.AppendLine("--- 6 tiradas horizontales mas largas (filas) ---");
        ReportRuns(sb, lum, px, W, H, false);

        return sb.ToString();
    }

    static void ReportRuns(StringBuilder sb, float[] lum, Color[] px, int W, int H, bool vertical)
    {
        int n = vertical ? W : H;
        int limit = vertical ? H : W;

        int[] runs = new int[n];
        int[] starts = new int[n];
        for (int i = 0; i < n; i++)
        {
            int best = 0, bestStart = 0, run = 0, start = 0;
            for (int j = 0; j < limit; j++)
            {
                float v = vertical ? lum[j * W + i] : lum[i * W + j];
                if (v > Threshold)
                {
                    if (run == 0) start = j;
                    run++;
                    if (run > best) { best = run; bestStart = start; }
                }
                else run = 0;
            }
            runs[i] = best; starts[i] = bestStart;
        }

        bool[] used = new bool[n];
        for (int k = 0; k < 6; k++)
        {
            int bi = -1, bv = 0;
            for (int i = 0; i < n; i++)
            {
                if (used[i] || runs[i] <= bv) continue;
                bi = i; bv = runs[i];
            }
            if (bi < 0) break;
            used[bi] = true;

            int mid = vertical ? (starts[bi] + bv / 2) : (starts[bi] + bv / 2);
            Color c = vertical ? px[mid * W + bi] : px[bi * W + mid];

            sb.AppendLine("  " + (vertical ? "x" : "yDesdeAbajo") + "=" + bi + " (" + (100f * bi / n).ToString("F1") + "%)" +
                          " tirada=" + bv + "/" + limit + " desde " + starts[bi] +
                          " color=" + Rgb(c));
        }
    }

    static string Rgb(Color c)
    {
        return "(" + (c.r * 255f).ToString("F0") + "," + (c.g * 255f).ToString("F0") + "," + (c.b * 255f).ToString("F0") + ")";
    }
}
