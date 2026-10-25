using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sonda temporal: captura con ScreenCapture.CaptureScreenshot y analiza los pixeles
/// del PNG resultante buscando tiradas largas de color identico (la cruz de 2 px).
/// Tambien permite apagar los gizmos del Game View para descartar que la cruz sea
/// un overlay del editor.
/// </summary>
public static class AuraShot2
{
    private const string Root = "Temp_";

    // ---------- Captura ----------

    public static string Capture(string tag)
    {
        if (!System.IO.Directory.Exists(Root)) System.IO.Directory.CreateDirectory(Root);
        string path = Root + "/aura_" + tag + ".png";
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        ScreenCapture.CaptureScreenshot(path);
        return "captured -> " + path + " | screen=" + Screen.width + "x" + Screen.height
            + " | gizmos=" + GizmoState();
    }

    // ---------- Gizmos del Game View ----------

    public static string GizmoState()
    {
        StringBuilder sb = new StringBuilder();
        System.Type gv = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gv == null) return "GameView type not found";
        UnityEngine.Object[] views = Resources.FindObjectsOfTypeAll(gv);
        foreach (UnityEngine.Object v in views)
        {
            PropertyInfo p = gv.GetProperty("showGizmos", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null) sb.Append("showGizmos=").Append(p.GetValue(v)).Append("; ");
            FieldInfo f = gv.GetField("m_ShowGizmos", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) sb.Append("m_ShowGizmos=").Append(f.GetValue(v)).Append("; ");
        }
        return sb.Length == 0 ? "no showGizmos member" : sb.ToString();
    }

    public static string SetGizmos(bool on)
    {
        System.Type gv = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gv == null) return "GameView type not found";
        UnityEngine.Object[] views = Resources.FindObjectsOfTypeAll(gv);
        int n = 0;
        foreach (UnityEngine.Object v in views)
        {
            PropertyInfo p = gv.GetProperty("showGizmos", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite) { p.SetValue(v, on); n++; }
            else
            {
                FieldInfo f = gv.GetField("m_ShowGizmos", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null) { f.SetValue(v, on); n++; }
            }
            if (v is EditorWindow w) w.Repaint();
        }
        return "gizmos=" + on + " views=" + n + " | " + GizmoState();
    }

    // ---------- Analisis ----------

    public static string Analyze(string path)
    {
        if (!System.IO.File.Exists(path)) return "missing: " + path;

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(System.IO.File.ReadAllBytes(path));
        int w = tex.width, h = tex.height;
        Color32[] px = tex.GetPixels32();
        tex = null;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(path + "  " + w + "x" + h);

        Color32 c0 = px[(h / 2) * w + (w / 2)];
        sb.AppendLine("centro=" + c0);

        // Columnas con tiradas verticales largas
        List<string> cols = new List<string>();
        for (int x = 0; x < w; x++)
        {
            int bestLen = 0, bestY = 0; Color32 bestC = default(Color32);
            int runStart = 0;
            for (int y = 1; y <= h; y++)
            {
                bool same = y < h && Same(px[y * w + x], px[(y - 1) * w + x]);
                if (!same)
                {
                    int len = y - runStart;
                    if (len > bestLen) { bestLen = len; bestY = runStart; bestC = px[runStart * w + x]; }
                    runStart = y;
                }
            }
            if (bestLen >= 60) cols.Add("x=" + x + " len=" + bestLen + " y0=" + bestY + " c=" + bestC);
        }
        sb.AppendLine("COLUMNAS(len>=60): " + (cols.Count == 0 ? "ninguna" : string.Join(" | ", ToArray(cols))));

        // Filas con tiradas horizontales largas
        List<string> rows = new List<string>();
        for (int y = 0; y < h; y++)
        {
            int bestLen = 0, bestX = 0; Color32 bestC = default(Color32);
            int runStart = 0;
            for (int x = 1; x <= w; x++)
            {
                bool same = x < w && Same(px[y * w + x], px[y * w + (x - 1)]);
                if (!same)
                {
                    int len = x - runStart;
                    if (len > bestLen) { bestLen = len; bestX = runStart; bestC = px[y * w + runStart]; }
                    runStart = x;
                }
            }
            if (bestLen >= 60) rows.Add("y_img=" + y + " len=" + bestLen + " x0=" + bestX + " c=" + bestC);
        }
        sb.AppendLine("FILAS(len>=60): " + (rows.Count == 0 ? "ninguna" : string.Join(" | ", ToArray(rows))));

        return sb.ToString();
    }

    private static string[] ToArray(List<string> l) { return l.ToArray(); }

    /// <summary>Muestreo puntual: solo los puntos donde deberia estar la cruz.</summary>
    public static string Probe(string path)
    {
        if (!System.IO.File.Exists(path)) return "missing: " + path;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(System.IO.File.ReadAllBytes(path));
        int w = tex.width, h = tex.height;
        Color32[] px = tex.GetPixels32();
        int cx = w / 2, cy = h / 2;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(path + " " + w + "x" + h + "  gizmos=" + GizmoState());
        // Brazo vertical: columna central, arriba de la tarjeta
        sb.Append("VERT ");
        for (int y = 5; y < 60; y += 10) sb.Append("y" + y + "=" + px[y * w + cx] + " ");
        sb.AppendLine();
        // Brazo horizontal: fila central, a la derecha de la tarjeta
        sb.Append("HORZ ");
        for (int x = w - 60; x < w - 5; x += 10) sb.Append("x" + x + "=" + px[cy * w + x] + " ");
        sb.AppendLine();
        sb.AppendLine("centro=" + px[cy * w + cx]);
        return sb.ToString();
    }

    /// <summary>Activa/desactiva un GameObject raiz por nombre (incluye inactivos).</summary>
    public static string Toggle(string name, bool active)
    {
        Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            if (t.name != name) continue;
            t.gameObject.SetActive(active);
            return name + " active=" + t.gameObject.activeSelf;
        }
        return "not found: " + name;
    }

    /// <summary>Muestrea una fila y una columna completas con paso fijo.</summary>
    public static string RowCol(string path, int row, int col, int step)
    {
        if (!System.IO.File.Exists(path)) return "missing: " + path;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(System.IO.File.ReadAllBytes(path));
        int w = tex.width, h = tex.height;
        Color32[] px = tex.GetPixels32();
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(path + " " + w + "x" + h);
        sb.Append("FILA " + row + ": ");
        for (int x = 0; x < w; x += step) sb.Append(x + ":" + Short(px[row * w + x]) + " ");
        sb.AppendLine();
        sb.Append("COL " + col + ": ");
        for (int y = 0; y < h; y += step) sb.Append(y + ":" + Short(px[y * w + col]) + " ");
        sb.AppendLine();
        return sb.ToString();
    }

    private static string Short(Color32 c)
    {
        return c.r.ToString("X2") + c.g.ToString("X2") + c.b.ToString("X2");
    }

    /// <summary>Volcado de raices de escena, camaras y estado del cursor.</summary>
    public static string Roots()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Screen " + Screen.width + "x" + Screen.height + " cursor.visible=" + Cursor.visible);

        foreach (Camera c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            sb.AppendLine("CAM '" + c.name + "' depth=" + c.depth + " clear=" + c.clearFlags + " mask=" + c.cullingMask
                + " pxRect=" + c.pixelRect + " rect=" + c.rect + " target=" + (c.targetTexture == null ? "null" : c.targetTexture.name)
                + " en=" + c.enabled);

        UnityEngine.SceneManagement.Scene sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (GameObject root in sc.GetRootGameObjects())
        {
            StringBuilder comps = new StringBuilder();
            Component[] all = root.GetComponents<Component>();
            for (int i = 0; i < all.Length; i++)
            {
                if (i > 0) comps.Append(", ");
                comps.Append(all[i] == null ? "MISSING" : all[i].GetType().FullName);
            }
            sb.AppendLine("ROOT '" + root.name + "' active=" + root.activeSelf + " hijos=" + root.transform.childCount + " {" + comps + "}");
        }

        return sb.ToString();
    }

    /// <summary>Mapa ASCII de una zona del PNG: ' '=fondo, '.'=oscuro, '+'=medio, '#'=claro.</summary>
    public static string Map(string path, int x0, int x1, int y0, int y1, int step)
    {
        if (!System.IO.File.Exists(path)) return "missing: " + path;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(System.IO.File.ReadAllBytes(path));
        int w = tex.width;
        Color32[] px = tex.GetPixels32();
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(path + " zona x[" + x0 + "," + x1 + "] y[" + y0 + "," + y1 + "] step=" + step);
        for (int y = y0; y <= y1; y += step)
        {
            sb.Append(y.ToString("000")).Append('|');
            for (int x = x0; x <= x1; x += step)
            {
                byte v = px[y * w + x].r;
                sb.Append(v < 12 ? ' ' : v < 60 ? '.' : v < 140 ? '+' : v < 220 ? '*' : '#');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>Activa/desactiva un componente de un GameObject por nombre.</summary>
    public static string ToggleComp(string goName, string typeName, bool on)
    {
        Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            if (t.name != goName) continue;
            foreach (Component c in t.GetComponents<Component>())
            {
                if (c == null || c.GetType().Name != typeName) continue;
                System.Type ct = c.GetType();
                System.Reflection.PropertyInfo en = ct.GetProperty("enabled");
                if (en != null && en.CanWrite) { en.SetValue(c, on); return goName + " " + typeName + " enabled=" + en.GetValue(c); }
                Behaviour b = c as Behaviour;
                if (b != null) { b.enabled = on; return goName + " " + typeName + " enabled=" + b.enabled; }
            }
            return "component not found: " + typeName + " on " + goName;
        }
        return "go not found: " + goName;
    }

    /// <summary>Apaga los gizmos del Game View y captura en el mismo frame.</summary>
    public static string Off(string tag)
    {
        System.Type gv = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        UnityEngine.Object[] views = Resources.FindObjectsOfTypeAll(gv);
        foreach (UnityEngine.Object v in views)
        {
            PropertyInfo p = gv.GetProperty("showGizmos", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite) p.SetValue(v, false);
            FieldInfo f = gv.GetField("m_ShowGizmos", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) f.SetValue(v, false);
        }
        return Capture(tag);
    }

    /// <summary>Posicion del raton dentro de la vista de juego.</summary>
    public static string Mouse()
    {
        string legacy = "n/a";
        try { legacy = UnityEngine.Input.mousePosition.ToString(); } catch (System.Exception e) { legacy = "err:" + e.GetType().Name; }
        string neu = "n/a";
        try
        {
            UnityEngine.InputSystem.Mouse m = UnityEngine.InputSystem.Mouse.current;
            neu = m == null ? "null" : m.position.ReadValue().ToString();
        }
        catch (System.Exception e) { neu = "err:" + e.GetType().Name; }
        return "legacy=" + legacy + " new=" + neu + " screen=" + Screen.width + "x" + Screen.height
            + " centro=" + (Screen.width / 2) + "," + (Screen.height / 2) + " cursorVisible=" + Cursor.visible;
    }

    /// <summary>Renderiza la camara a una RenderTexture (como un jugador) y busca la reticula.</summary>
    public static string RenderCheck()
    {
        Camera cam = Camera.main;
        if (cam == null) return "no main camera";
        int w = Screen.width, h = Screen.height;
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        RenderTexture prev = RenderTexture.active;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = null;

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        rt.Release();

        Color32[] px = tex.GetPixels32();
        System.IO.File.WriteAllBytes(Root + "/aura_rt.png", tex.EncodeToPNG());

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("RT " + w + "x" + h + " (filas desde abajo) guardado en " + Root + "/aura_rt.png");
        sb.Append("FILA centro y=" + (h / 2) + ": ");
        for (int x = w - 60; x < w - 5; x += 10) sb.Append(x + ":" + Short(px[(h / 2) * w + x]) + " ");
        sb.AppendLine();
        sb.Append("COL centro x=" + (w / 2) + ": ");
        for (int y = 5; y < 60; y += 10) sb.Append(y + ":" + Short(px[y * w + (w / 2)]) + " ");
        sb.AppendLine();
        sb.AppendLine("centro=" + px[(h / 2) * w + (w / 2)]);
        return sb.ToString();
    }

    /// <summary>Abre el panel de creditos del menu (para revisarlo en captura).</summary>
    public static string OpenCredits()
    {
        MainMenuUI ui = UnityEngine.Object.FindFirstObjectByType<MainMenuUI>();
        if (ui == null) return "MainMenuUI no encontrado";
        MethodInfo m = typeof(MainMenuUI).GetMethod("OpenCredits", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m == null) return "OpenCredits no encontrado";
        m.Invoke(ui, null);
        return "panel de creditos abierto";
    }

    private static bool Same(Color32 a, Color32 b)
    {
        return Mathf.Abs(a.r - b.r) <= 1 && Mathf.Abs(a.g - b.g) <= 1 && Mathf.Abs(a.b - b.b) <= 1;
    }
}
