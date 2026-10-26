using UnityEditor;
using UnityEngine;

/// <summary>
/// Runner de consola para GameSceneBuilder (se usa desde Aura).
/// No contiene lógica propia: solo invoca el builder y guarda los assets.
/// </summary>
public static class AuraBuilderRunner
{
    public static void Execute()
    {
        double t0 = EditorApplication.timeSinceStartup;
        GameSceneBuilder.BuildAll();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Aura] GameSceneBuilder terminó en " + (EditorApplication.timeSinceStartup - t0).ToString("F1") + " s");
    }

    public static void ReportRenderers()
    {
        SpriteRenderer[] all = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("[Aura] SpriteRenderers: " + all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            SpriteRenderer r = all[i];
            sb.AppendLine(string.Format("{0} | pos={1} | layer={2} order={3} | sprite={4} | color={5} | enabled={6} | activeInHierarchy={7}",
                r.name, r.transform.position, r.sortingLayerName, r.sortingOrder,
                r.sprite != null ? r.sprite.name : "NULL", r.color, r.enabled, r.gameObject.activeInHierarchy));
        }
        Debug.Log(sb.ToString());
    }

    public static void ReportScenes()
    {
        int n = EditorBuildSettings.scenes == null ? 0 : EditorBuildSettings.scenes.Length;
        Debug.Log("[Aura] Escenas en Build Settings: " + n);
        for (int i = 0; i < n; i++)
            Debug.Log("  [" + i + "] " + EditorBuildSettings.scenes[i].path + " enabled=" + EditorBuildSettings.scenes[i].enabled);
    }
}
