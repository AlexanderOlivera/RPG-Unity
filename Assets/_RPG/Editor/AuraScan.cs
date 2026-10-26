using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sonda temporal: busca la fuente de la cruz de 2 px centrada en el canvas del
/// menu principal. Enumera canvas, Renderers finos que cubren el centro de la
/// pantalla, Graphics finos y MonoBehaviour con OnGUI / OnPostRender / OnRenderImage.
/// </summary>
public static class AuraScan
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Screen " + Screen.width + "x" + Screen.height + "  play=" + Application.isPlaying);

        Vector3 centre = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Camera main = Camera.main;
        sb.AppendLine("mainCam=" + (main == null ? "null"
            : main.name + " pxRect=" + main.pixelRect + " target=" + (main.targetTexture == null ? "null" : main.targetTexture.name)));

        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            RectTransform rt = c.GetComponent<RectTransform>();
            sb.AppendLine("CANVAS " + c.name + " root=" + c.isRootCanvas + " mode=" + c.renderMode
                + " order=" + c.sortingOrder + " scale=" + c.scaleFactor + " rect=" + rt.rect
                + " act=" + c.gameObject.activeInHierarchy + " en=" + c.enabled
                + " cam=" + (c.worldCamera == null ? "null" : c.worldCamera.name));
        }

        // --- Renderers finos que cubren el centro ---
        int nearCount = 0;
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r.GetType().FullName.Contains("Graphic")) continue;
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (main == null) continue;

            Vector3 mn = r.bounds.min, mx = r.bounds.max;
            float z = (mn.z + mx.z) * 0.5f;
            Vector3 p0 = main.WorldToScreenPoint(new Vector3(mn.x, mn.y, z));
            Vector3 p1 = main.WorldToScreenPoint(new Vector3(mx.x, mx.y, z));
            float x0 = Mathf.Min(p0.x, p1.x), x1 = Mathf.Max(p0.x, p1.x);
            float y0 = Mathf.Min(p0.y, p1.y), y1 = Mathf.Max(p0.y, p1.y);

            bool thin = (x1 - x0) <= 6f || (y1 - y0) <= 6f;
            bool covers = centre.x >= x0 - 4f && centre.x <= x1 + 4f && centre.y >= y0 - 4f && centre.y <= y1 + 4f;
            if (thin && covers)
            {
                nearCount++;
                sb.AppendLine("NEAR " + r.GetType().Name + " '" + r.name + "' pxX[" + x0.ToString("F0") + "," + x1.ToString("F0")
                    + "] pxY[" + y0.ToString("F0") + "," + y1.ToString("F0") + "] layer=" + r.sortingLayerName
                    + "/" + r.sortingOrder + " sortLayerID=" + r.sortingLayerID);
            }
        }
        sb.AppendLine("nearCount=" + nearCount);

        // --- Graphics finos que cubren el centro ---
        int gNear = 0;
        foreach (Graphic g in Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None))
        {
            if (g == null || !g.enabled || !g.gameObject.activeInHierarchy) continue;
            Vector3[] corners = new Vector3[4];
            g.rectTransform.GetWorldCorners(corners);
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                x0 = Mathf.Min(x0, corners[i].x); x1 = Mathf.Max(x1, corners[i].x);
                y0 = Mathf.Min(y0, corners[i].y); y1 = Mathf.Max(y1, corners[i].y);
            }
            bool thin = (x1 - x0) <= 6f || (y1 - y0) <= 6f;
            bool covers = centre.x >= x0 - 4f && centre.x <= x1 + 4f && centre.y >= y0 - 4f && centre.y <= y1 + 4f;
            if (thin && covers)
            {
                gNear++;
                sb.AppendLine("GNEAR " + g.GetType().Name + " '" + g.name + "' pxX[" + x0.ToString("F0") + "," + x1.ToString("F0")
                    + "] pxY[" + y0.ToString("F0") + "," + y1.ToString("F0") + "] color=" + g.color);
            }
        }
        sb.AppendLine("graphicNear=" + gNear);

        // --- Jerarquia completa del canvas ---
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            sb.AppendLine("---- HIERARQUIA " + c.name + " ----");
            DumpNode(c.transform, 0, sb);
        }

        // --- Metodos que dibujan fuera del sistema de UI ---
        HashSet<string> seen = new HashSet<string>();
        string[] hooks = { "OnGUI", "OnPostRender", "OnRenderImage", "OnDrawGizmos", "OnDrawGizmosSelected" };
        foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            System.Type t = mb.GetType();
            foreach (string hook in hooks)
            {
                MethodInfo m = t.GetMethod(hook, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null && seen.Add(t.FullName + "#" + hook))
                    sb.AppendLine("HOOK " + hook + " " + t.FullName + " on '" + mb.name + "'");
            }
        }

        sb.AppendLine("end");
        string result = sb.ToString();
        Debug.Log("[AuraScan]\n" + result);
        return result;
    }

    private static void DumpNode(Transform t, int depth, StringBuilder sb)
    {
        StringBuilder comps = new StringBuilder();
        Component[] all = t.GetComponents<Component>();
        for (int i = 0; i < all.Length; i++)
        {
            if (i > 0) comps.Append(", ");
            comps.Append(all[i] == null ? "MISSING" : all[i].GetType().Name);
        }

        sb.Append(' ', depth * 2).Append("- ").Append(t.name)
          .Append(t.gameObject.activeSelf ? "" : " [inactivo]")
          .Append(" {").Append(comps).Append('}');

        Graphic g = t.GetComponent<Graphic>();
        if (g != null) sb.Append(" color=").Append(g.color).Append(" rt=").Append(g.rectTransform.rect);
        sb.AppendLine();

        for (int i = 0; i < t.childCount; i++) DumpNode(t.GetChild(i), depth + 1, sb);
    }
}
