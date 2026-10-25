using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Regenera solo la escena del menú principal (no toca las demás).</summary>
public static class AuraMenuBuild
{
    public static void Execute()
    {
        GameSceneBuilder.BuildMainMenu();
        EditorSceneManager.OpenScene("Assets/_RPG/Scenes/00_MainMenu.unity");
        Debug.Log("[Aura] Menú principal regenerado.");
    }
}
