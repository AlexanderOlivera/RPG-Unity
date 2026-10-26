using UnityEngine;

[System.Serializable]
public class SceneStory
{
    public string sceneName;
    public string title;
    [TextArea(2, 6)]
    public string[] lines;
}

/// <summary>Textos de historia por escenario (se carga desde Resources/DialogueDatabase).</summary>
public class DialogueDatabase : ScriptableObject
{
    public SceneStory[] stories;

    public SceneStory Get(string sceneName)
    {
        if (stories == null) return null;
        for (int i = 0; i < stories.Length; i++)
            if (stories[i] != null && stories[i].sceneName == sceneName) return stories[i];
        return null;
    }
}
