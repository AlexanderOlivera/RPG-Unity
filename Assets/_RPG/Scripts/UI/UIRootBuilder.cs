using UnityEngine;

/// <summary>Fábrica del HUD (se invoca automáticamente en cada escena de juego).</summary>
public static class UIRootBuilder
{
    public static UIRoot Build()
    {
        if (UIRoot.Instance != null) return UIRoot.Instance;

        GameObject go = new GameObject("~UIRoot");
        UIRoot root = go.AddComponent<UIRoot>();
        return root;
    }
}
