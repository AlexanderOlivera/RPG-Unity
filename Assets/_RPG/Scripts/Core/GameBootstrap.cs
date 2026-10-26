using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Arranque automático: crea los gestores persistentes y prepara cada escena
/// (UI, jugador y registros) sin necesidad de cablear nada a mano.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SetupScene();
    }

    public static void EnsureCoreObjects()
    {
        if (GameManager.Instance == null) new GameObject("~GameManager").AddComponent<GameManager>();
        if (AudioManager.Instance == null) new GameObject("~AudioManager").AddComponent<AudioManager>();
        EnsureEventSystem();
    }

    public static void SetupScene()
    {
        EnsureCoreObjects();

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == GameManager.MainMenuScene) return;
        if (sceneName == GameManager.CreditsScene) return;

        if (UIRoot.Instance == null) UIRootBuilder.Build();
        if (QuestTracker.Instance == null) new GameObject("~QuestTracker").AddComponent<QuestTracker>();

        if (GameManager.Instance != null) GameManager.Instance.SetupPlayerForScene();
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        GameObject go = new GameObject("~EventSystem");
        go.AddComponent<UnityEngine.EventSystems.EventSystem>();
        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }
}
