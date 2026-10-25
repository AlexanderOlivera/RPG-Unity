using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor global: pausa, derrota, victoria, cambios de escena y persistencia
/// de la vida del jugador entre escenarios. Se crea automáticamente.
/// </summary>
public class GameManager : MonoBehaviour
{
    public const string MainMenuScene = "00_MainMenu";
    public const string FirstScene = "01_Bosque";
    public const string CreditsScene = "06_Creditos";

    public static GameManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }
    public static bool IsGameOver { get; private set; }
    public static bool IsVictory { get; private set; }
    public static string CurrentSceneName { get; private set; }

    public static event Action OnSessionChanged;
    public static void NotifyChanged()
    {
        if (OnSessionChanged != null) OnSessionChanged();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Time.timeScale = 1f;
        IsPaused = false;
        IsGameOver = false;
        IsVictory = false;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void Update()
    {
        // Respaldo por si algún objeto UI no está presente.
        if (Input.GetKeyDown(KeyCode.Escape) && !IsGameOver && !IsVictory && UIRoot.Instance == null)
            SetPaused(!IsPaused);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CurrentSceneName = scene.name;
        Time.timeScale = 1f;
        IsPaused = false;
        // El estado de diálogo es estático: si no se limpia al cargar, los aldeanos
        // se quedan congelados (NpcWanderer se frena con UIRoot.DialogueOpen).
        UIRoot.ResetUiState();

        PlayerController existingPlayer = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (existingPlayer != null)
        {
            Health existingHealth = existingPlayer.GetComponent<Health>();
            if (existingHealth != null) existingHealth.OnHealthChanged -= HandlePlayerHealthChanged;
            if (existingHealth != null) existingHealth.OnDied -= HandlePlayerDied;
        }

        GameBootstrap.SetupScene();
    }

    /// <summary>Aplica la vida persistente al jugador de la escena actual.</summary>
    public void SetupPlayerForScene()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic();

        PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        Health h = player.GetComponent<Health>();
        if (h == null) return;

        h.maxHealth = GameSession.PlayerMaxHealth;
        h.SetHealth(GameSession.PlayerHealth > 0 ? GameSession.PlayerHealth : GameSession.PlayerMaxHealth);
        h.OnHealthChanged -= HandlePlayerHealthChanged;
        h.OnHealthChanged += HandlePlayerHealthChanged;
        h.OnDied -= HandlePlayerDied;
        h.OnDied += HandlePlayerDied;
        NotifyChanged();
    }

    void HandlePlayerHealthChanged(Health h)
    {
        GameSession.PlayerHealth = h.CurrentHealth;
        NotifyChanged();
    }

    void HandlePlayerDied(Health h)
    {
        GameOver();
    }

    public static void SetPaused(bool value)
    {
        if (IsGameOver || IsVictory) return;
        IsPaused = value;
        Time.timeScale = value ? 0f : 1f;
        if (UIRoot.Instance != null) UIRoot.Instance.Refresh();
    }

    public static void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    public static void GameOver()
    {
        if (IsGameOver || IsVictory) return;
        IsGameOver = true;
        IsPaused = true;
        Time.timeScale = 0f;
        if (UIRoot.Instance != null) UIRoot.Instance.ShowGameOver();
    }

    public static void NotifyBossDefeated()
    {
        GameSession.BossDefeated = true;
        NotifyChanged();
    }

    public static void Victory()
    {
        if (IsVictory) return;
        IsVictory = true;
        IsPaused = true;
        GameSession.Won = true;
        Time.timeScale = 0f;
        if (UIRoot.Instance != null) UIRoot.Instance.ShowVictory();
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneByIndex(int buildIndex)
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(buildIndex);
    }

    public void RestartScene()
    {
        IsGameOver = false;
        IsVictory = false;
        IsPaused = false;
        Time.timeScale = 1f;
        UIRoot.ResetUiState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RestartCurrentFromCheckpoint()
    {
        GameSession.PlayerHealth = GameSession.PlayerMaxHealth;
        RestartScene();
    }

    public void LoadMainMenu()
    {
        IsGameOver = false;
        IsVictory = false;
        IsPaused = false;
        Time.timeScale = 1f;
        UIRoot.ResetUiState();
        SceneManager.LoadScene(MainMenuScene);
    }

    /// <summary>Abre la escena de créditos finales (desde la victoria o el menú).</summary>
    public void LoadCredits()
    {
        IsGameOver = false;
        IsVictory = false;
        IsPaused = false;
        Time.timeScale = 1f;
        UIRoot.ResetUiState();
        SceneManager.LoadScene(CreditsScene);
    }

    public void StartNewGame()
    {
        GameSession.Reset();
        IsGameOver = false;
        IsVictory = false;
        IsPaused = false;
        Time.timeScale = 1f;
        UIRoot.ResetUiState();
        SceneManager.LoadScene(FirstScene);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
