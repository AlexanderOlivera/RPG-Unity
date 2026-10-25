using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sonda temporal: vuelca todos los Graphic del canvas con su rect en pixeles de pantalla
/// para localizar elementos degenerados (recticolapso, cruces, lineas sueltas).
/// </summary>
public static class AuraUiProbe
{
    public static string Run()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Screen=" + Screen.width + "x" + Screen.height);

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        sb.AppendLine("Canvas=" + canvases.Length);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            sb.AppendLine("  " + c.name + " mode=" + c.renderMode + " sortOrder=" + c.sortingOrder +
                          " scale=" + c.scaleFactor.ToString("F3") +
                          " rect=" + c.GetComponent<RectTransform>().rect.size +
                          " enabled=" + c.enabled);
        }

        Graphic[] gs = Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None);
        sb.AppendLine("Graphics=" + gs.Length);

        sb.AppendLine("--- SOSPECHOSOS: rect pequeno o muy alargado, o centrado en pantalla ---");
        for (int i = 0; i < gs.Length; i++)
        {
            Graphic g = gs[i];
            if (g == null) continue;
            RectTransform rt = g.rectTransform;

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            float minX = corners[0].x, maxX = corners[2].x;
            float minY = corners[0].y, maxY = corners[2].y;
            float w = maxX - minX;
            float h = maxY - minY;

            bool thin = w < 14f || h < 14f;
            bool crossesCentre = minX < Screen.width * 0.5f && maxX > Screen.width * 0.5f &&
                                 minY < Screen.height * 0.5f && maxY > Screen.height * 0.5f;
            bool tiny = w < 14f && h < 14f;

            if (!(thin || tiny)) continue;

            string kind = g.GetType().Name;
            string sprite = "";
            Image img = g as Image;
            if (img != null)
                sprite = " sprite=" + (img.sprite != null ? img.sprite.name + "(" +
                         img.sprite.rect.width + "x" + img.sprite.rect.height + ") tipo=" + img.type : "null");
            Text t = g as Text;
            string text = t != null ? " txt=\"" + t.text + "\"" : "";

            sb.AppendLine("  " + Path(rt) + " [" + kind + "]" + sprite + text);
            sb.AppendLine("     color=" + Col(g.color) + " active=" + g.gameObject.activeInHierarchy +
                          " raycast=" + g.raycastTarget);
            sb.AppendLine("     rect local=" + rt.rect + " pivot=" + rt.pivot +
                          " sizeDelta=" + rt.sizeDelta);
            sb.AppendLine("     pantalla x[" + minX.ToString("F1") + "," + maxX.ToString("F1") +
                          "] yDesdeAbajo[" + minY.ToString("F1") + "," + maxY.ToString("F1") + "]  w=" + w.ToString("F1") + " h=" + h.ToString("F1") +
                          (tiny ? "  <<< MINUSCULO" : "") +
                          (crossesCentre ? "  <<< CRUZA EL CENTRO" : ""));
        }

        // Resumen de los elementos dorados (por si el culpable no es fino)
        sb.AppendLine("--- ELEMENTOS DORADOS visibles ---");
        for (int i = 0; i < gs.Length; i++)
        {
            Graphic g = gs[i];
            if (g == null || !g.gameObject.activeInHierarchy) continue;
            Color c = g.color;
            if (c.a < 0.05f) continue;
            if (!(c.r > 0.75f && c.g > 0.6f && c.b < 0.75f && c.b > 0.3f)) continue;

            Vector3[] corners = new Vector3[4];
            g.rectTransform.GetWorldCorners(corners);
            sb.AppendLine("  " + Path(g.rectTransform) + " [" + g.GetType().Name + "] color=" + Col(c) +
                          " w=" + (corners[2].x - corners[0].x).ToString("F1") +
                          " h=" + (corners[2].y - corners[0].y).ToString("F1") +
                          " x[" + corners[0].x.ToString("F0") + "," + corners[2].x.ToString("F0") + "]" +
                          " y[" + corners[0].y.ToString("F0") + "," + corners[2].y.ToString("F0") + "]");
        }

        return sb.ToString();
    }

    static string Col(Color c)
    {
        return "(" + (c.r * 255f).ToString("F0") + "," + (c.g * 255f).ToString("F0") + "," +
               (c.b * 255f).ToString("F0") + ",a" + c.a.ToString("F2") + ")";
    }

    static string Path(RectTransform rt)
    {
        string p = rt.name;
        Transform t = rt.parent;
        int guard = 0;
        while (t != null && guard++ < 4) { p = t.name + "/" + p; t = t.parent; }
        return p;
    }
}
