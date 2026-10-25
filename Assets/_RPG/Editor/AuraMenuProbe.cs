using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Sonda de diagnostico del menu principal. Analiza el frame renderizado para localizar
/// los bordes duros (lineas) y los cruza con los bounds de los SpriteRenderer presentes.
/// Temporal: borrar cuando el artefacto este resuelto.
/// </summary>
public static class AuraMenuProbe
{
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();

        Camera cam = Camera.main;
        if (cam == null) return "sin camara";

        float hh = cam.orthographicSize;
        float hw = hh * cam.aspect;
        float camX = cam.transform.position.x;
        float camY = cam.transform.position.y;

        sb.AppendLine("CAM pos=" + cam.transform.position + " size=" + hh + " aspect=" + cam.aspect.ToString("F3"));
        sb.AppendLine("CAM rect x[" + (camX - hw).ToString("F2") + "," + (camX + hw).ToString("F2") +
                      "] y[" + (camY - hh).ToString("F2") + "," + (camY + hh).ToString("F2") + "]");

        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        sb.AppendLine("Cameras=" + cams.Length);
        for (int i = 0; i < cams.Length; i++)
        {
            Camera c = cams[i];
            sb.AppendLine("  " + c.name + " depth=" + c.depth + " rect=" + c.rect +
                          " ort=" + c.orthographic + " size=" + c.orthographicSize +
                          " clear=" + c.clearFlags + " bg=" + c.backgroundColor +
                          " cull=" + c.cullingMask +
                          " tex=" + (c.targetTexture != null ? c.targetTexture.name : "null") +
                          " enabled=" + c.enabled);
        }

        // ---------- Analisis de pixeles del frame ----------
        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
        if (shot == null) { sb.AppendLine("SIN CAPTURA"); return sb.ToString(); }

        int W = shot.width;
        int H = shot.height;
        sb.AppendLine("Shot=" + W + "x" + H);
        Color[] px = shot.GetPixels(); // fila 0 = borde inferior

        float[] colScore = new float[W];
        float[] rowScore = new float[H];

        for (int x = 1; x < W; x++)
        {
            float s = 0f;
            for (int y = 0; y < H; y++)
            {
                Color a = px[y * W + x];
                Color b = px[y * W + x - 1];
                s += Mathf.Abs((a.r + a.g + a.b) - (b.r + b.g + b.b));
            }
            colScore[x] = s / H;
        }

        for (int y = 1; y < H; y++)
        {
            float s = 0f;
            for (int x = 0; x < W; x++)
            {
                Color a = px[y * W + x];
                Color b = px[(y - 1) * W + x];
                s += Mathf.Abs((a.r + a.g + a.b) - (b.r + b.g + b.b));
            }
            rowScore[y] = s / W;
        }

        sb.AppendLine("--- COLUMNAS con borde mas fuerte ---");
        List<int> bestCols = TopPeaks(colScore, 8);
        for (int i = 0; i < bestCols.Count; i++)
        {
            int x = bestCols[i];
            float wx = camX - hw + (x / (float)W) * hw * 2f;
            Color left = px[(H / 2) * W + Mathf.Max(0, x - 2)];
            Color right = px[(H / 2) * W + Mathf.Min(W - 1, x + 2)];
            sb.AppendLine("col=" + x + " (" + (100f * x / W).ToString("F1") + "%) score=" + colScore[x].ToString("F3") +
                          " worldX=" + wx.ToString("F2") +
                          " izq=" + Fmt(left) + " der=" + Fmt(right));
        }

        sb.AppendLine("--- FILAS con borde mas fuerte ---");
        List<int> bestRows = TopPeaks(rowScore, 8);
        for (int i = 0; i < bestRows.Count; i++)
        {
            int y = bestRows[i]; // desde abajo
            float wy = camY - hh + (y / (float)H) * hh * 2f;
            int sy = H - 1 - y; // desde arriba
            Color below = px[Mathf.Max(0, y - 2) * W + W / 4];
            Color above = px[Mathf.Min(H - 1, y + 2) * W + W / 4];
            sb.AppendLine("filaDesdeAbajo=" + y + " (desdeArriba=" + sy + ", " + (100f * sy / H).ToString("F1") + "%) score=" + rowScore[y].ToString("F3") +
                          " worldY=" + wy.ToString("F2") +
                          " abajo=" + Fmt(below) + " arriba=" + Fmt(above));
        }

        // ---------- Objetos cuyos bounds coinciden con esos bordes ----------
        SpriteRenderer[] srs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        sb.AppendLine("SpriteRenderers totales=" + srs.Length);

        const float tol = 0.30f;
        sb.AppendLine("--- SR con un borde cerca de una linea vertical detectada ---");
        for (int i = 0; i < bestCols.Count && i < 4; i++)
        {
            int x = bestCols[i];
            float wx = camX - hw + (x / (float)W) * hw * 2f;
            int hits = 0;
            for (int s = 0; s < srs.Length; s++)
            {
                Bounds b = srs[s].bounds;
                if (Mathf.Abs(b.min.x - wx) < tol || Mathf.Abs(b.max.x - wx) < tol)
                {
                    if (hits < 8)
                        sb.AppendLine("  x~" + wx.ToString("F2") + " :: " + Name(srs[s]) + " ord=" + srs[s].sortingOrder +
                                      " b=x[" + b.min.x.ToString("F2") + "," + b.max.x.ToString("F2") + "] y[" + b.min.y.ToString("F2") + "," + b.max.y.ToString("F2") + "]");
                    hits++;
                }
            }
            sb.AppendLine("  x~" + wx.ToString("F2") + " total=" + hits);
        }

        sb.AppendLine("--- SR con un borde cerca de una linea horizontal detectada ---");
        for (int i = 0; i < bestRows.Count && i < 4; i++)
        {
            int y = bestRows[i];
            float wy = camY - hh + (y / (float)H) * hh * 2f;
            int hits = 0;
            for (int s = 0; s < srs.Length; s++)
            {
                Bounds b = srs[s].bounds;
                if (Mathf.Abs(b.min.y - wy) < tol || Mathf.Abs(b.max.y - wy) < tol)
                {
                    if (hits < 8)
                        sb.AppendLine("  y~" + wy.ToString("F2") + " :: " + Name(srs[s]) + " ord=" + srs[s].sortingOrder +
                                      " b=x[" + b.min.x.ToString("F2") + "," + b.max.x.ToString("F2") + "] y[" + b.min.y.ToString("F2") + "," + b.max.y.ToString("F2") + "]");
                    hits++;
                }
            }
            sb.AppendLine("  y~" + wy.ToString("F2") + " total=" + hits);
        }

        // ---------- Luces y mascaras ----------
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        sb.AppendLine("Luces=" + lights.Length);
        for (int i = 0; i < lights.Length; i++)
            sb.AppendLine("  " + lights[i].name + " type=" + lights[i].type + " range=" + lights[i].range + " intensity=" + lights[i].intensity);

        int masks = 0;
        foreach (SpriteRenderer sr in srs) if (sr.maskInteraction != SpriteMaskInteraction.None) masks++;
        sb.AppendLine("SR con maskInteraction activa=" + masks);

        return sb.ToString();
    }

    static string Fmt(Color c)
    {
        return "(" + c.r.ToString("F3") + "," + c.g.ToString("F3") + "," + c.b.ToString("F3") + ")";
    }

    static string Name(SpriteRenderer sr)
    {
        Transform t = sr.transform;
        string path = t.name;
        Transform p = t.parent;
        int guard = 0;
        while (p != null && guard++ < 3) { path = p.name + "/" + path; p = p.parent; }
        return path;
    }

    static List<int> TopPeaks(float[] score, int count)
    {
        List<int> result = new List<int>();
        int n = score.Length;

        for (int i = 2; i < n - 1; i++)
        {
            if (score[i] <= 0f) continue;
            bool localMax = score[i] >= score[i - 1] && score[i] >= score[i + 1];
            if (!localMax) continue;

            float neighbourAvg = 0f;
            int taken = 0;
            for (int k = -4; k <= 4; k++)
            {
                int j = i + k;
                if (Mathf.Abs(k) <= 1 || j < 0 || j >= n) continue;
                neighbourAvg += score[j];
                taken++;
            }
            if (taken > 0 && score[i] < neighbourAvg / taken * 1.6f) continue; // no destaca: textura normal

            bool inserted = false;
            for (int r = 0; r < result.Count; r++)
            {
                if (score[i] > score[result[r]]) { result.Insert(r, i); inserted = true; break; }
            }
            if (!inserted && result.Count < count) result.Add(i);
            if (result.Count > count) result.RemoveAt(result.Count - 1);
        }

        return result;
    }
}
