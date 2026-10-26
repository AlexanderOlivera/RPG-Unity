using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Sonda temporal: analiza una captura del Game View y localiza lineas reales
/// (columnas/filas con muchos pixeles claros seguidos) en lugar de bordes sueltos.
/// No requiere Play Mode.
/// </summary>
public static class AuraImageProbe
{
    const string Path = @"C:\Users\leona\AppData\Local\AuraForUnity\CapturedImages\c19c4c86d0a94efe\3837ead3-a380-422e-a9d1-e53c3cb80422.jpeg";

    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        if (!File.Exists(Path)) return "NO EXISTE: " + Path;

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(Path))) return "No se pudo decodificar";
        tex.wrapMode = TextureWrapMode.Clamp;

        int W = tex.width;
        int H = tex.height;
        sb.AppendLine("IMG=" + W + "x" + H + " (fila 0 = ABAJO)");

        Color[] px = tex.GetPixels();
        float[] lum = new float[W * H];
        for (int i = 0; i < lum.Length; i++)
            lum[i] = px[i].r * 0.299f + px[i].g * 0.587f + px[i].b * 0.114f;

        // ---- Columnas con muchos pixeles claros (lineas verticales) ----
        sb.AppendLine("--- COLUMNAS: nº de pixeles con lum>0.12 ---");
        int[] brightCol = new int[W];
        for (int x = 0; x < W; x++)
        {
            int c = 0;
            for (int y = 0; y < H; y++) if (lum[y * W + x] > 0.12f) c++;
            brightCol[x] = c;
        }
        ReportTopCounts(sb, brightCol, "col", W, 12);

        // ---- Filas con muchos pixeles claros (lineas horizontales) ----
        sb.AppendLine("--- FILAS: nº de pixeles con lum>0.12 ---");
        int[] brightRow = new int[H];
        for (int y = 0; y < H; y++)
        {
            int c = 0;
            for (int x = 0; x < W; x++) if (lum[y * W + x] > 0.12f) c++;
            brightRow[y] = c;
        }
        ReportTopCounts(sb, brightRow, "filaDesdeAbajo", H, 12);

        // ---- Perfil detallado de la columna mas "llena" ----
        int bestCol = ArgMax(brightCol);
        sb.AppendLine("--- COLUMNA " + bestCol + " (" + (100f * bestCol / W).ToString("F1") + "%) perfil vertical (cada 6 px) ---");
        StringBuilder prof = new StringBuilder();
        for (int y = H - 1; y >= 0; y -= 6)
        {
            prof.Append("y" + y + "=" + (lum[y * W + bestCol] * 255f).ToString("F0") + " ");
            if ((H - 1 - y) % 60 > 54) { sb.AppendLine(prof.ToString()); prof.Length = 0; }
        }
        sb.AppendLine(prof.ToString());

        // ---- Perfil detallado de la fila mas "llena" ----
        int bestRow = ArgMax(brightRow);
        sb.AppendLine("--- FILA " + bestRow + " (" + (100f * bestRow / H).ToString("F1") + "% desde arriba=" + (100f * (H - 1 - bestRow) / H).ToString("F1") + "%) perfil horizontal (cada 8 px) ---");
        prof.Length = 0;
        for (int x = 0; x < W; x += 8)
        {
            prof.Append("x" + x + "=" + (lum[bestRow * W + x] * 255f).ToString("F0") + " ");
            if (x % 480 > 440) { sb.AppendLine(prof.ToString()); prof.Length = 0; }
        }
        sb.AppendLine(prof.ToString());

        // ---- Cuantos pixeles tienen color dorado ----
        sb.AppendLine("--- PIXELES DORADOS (r>0.7, g>0.55, b en 0.25..0.75) agrupados por columna ---");
        int[] goldCol = new int[W];
        for (int x = 0; x < W; x++)
        {
            int c = 0;
            for (int y = 0; y < H; y++)
            {
                Color p = px[y * W + x];
                if (p.r > 0.7f && p.g > 0.55f && p.b > 0.25f && p.b < 0.75f) c++;
            }
            goldCol[x] = c;
        }
        ReportTopCounts(sb, goldCol, "colDorada", W, 10);

        return sb.ToString();
    }

    static int ArgMax(int[] a)
    {
        int best = 0;
        for (int i = 1; i < a.Length; i++) if (a[i] > a[best]) best = i;
        return best;
    }

    static void ReportTopCounts(StringBuilder sb, int[] counts, string label, int n, int top)
    {
        bool[] used = new bool[n];
        for (int k = 0; k < top; k++)
        {
            int best = -1, bestVal = 0;
            for (int i = 0; i < n; i++)
            {
                if (used[i] || counts[i] <= bestVal) continue;
                best = i; bestVal = counts[i];
            }
            if (best < 0) break;
            used[best] = true;
            sb.AppendLine("  " + label + "=" + best + " (" + (100f * best / n).ToString("F1") + "%) claros=" + bestVal);
        }
    }
}
