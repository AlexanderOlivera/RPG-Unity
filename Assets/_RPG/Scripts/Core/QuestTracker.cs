using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Objetivos de la historia: muestra la introducción de cada escenario y
/// calcula el objetivo actual según los fragmentos recogidos.
/// </summary>
public class QuestTracker : MonoBehaviour
{
    public static QuestTracker Instance { get; private set; }

    public int totalFragments = 4;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        Invoke("ShowIntro", 0.4f);
    }

    void ShowIntro()
    {
        if (UIRoot.Instance == null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        DialogueDatabase db = Resources.Load<DialogueDatabase>("DialogueDatabase");
        string title = "Objetivo";
        string text = ObjectiveText();

        if (db != null)
        {
            SceneStory story = db.Get(sceneName);
            if (story != null && story.lines != null && story.lines.Length > 0)
            {
                title = story.title;
                text = string.Join("\n", story.lines) + "\n\n" + ObjectiveText();
            }
        }

        UIRoot.Instance.ShowBanner(title, text, 7f);
    }

    public string ObjectiveText()
    {
        if (GameSession.BossDefeated) return "Sello restaurado: la Arboleda vuelve a respirar.";
        if (GameSession.Fragments < totalFragments)
            return "Fragmentos del Sello: " + GameSession.Fragments + "/" + totalFragments +
                   ". Explora los escenarios y habla con los aldeanos.";
        return "¡Tienes los 4 fragmentos! Cruza al Santuario y derrota al Guardián.";
    }
}
