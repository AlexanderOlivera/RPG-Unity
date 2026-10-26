using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AuraBiomeAudit
{
    static readonly string[] Scenes =
    {
        "Assets/_RPG/Scenes/01_Bosque.unity",
        "Assets/_RPG/Scenes/02_Aldea.unity",
        "Assets/_RPG/Scenes/03_Cuevas.unity",
        "Assets/_RPG/Scenes/04_Ruinas.unity",
        "Assets/_RPG/Scenes/05_Santuario.unity"
    };

    // Familias que NO deben aparecer en un ámbito concreto.
    static readonly string[] PlantWords = { "bush", "grass", "flower", "tree" };
    static readonly string[] VillageWords = { "barrel", "crate", "pot", "well", "wooden gate", "road sign", "chest" };

    public static string Run()
    {
        StringBuilder sb = new StringBuilder();

        for (int s = 0; s < Scenes.Length; s++)
        {
            EditorSceneManager.OpenScene(Scenes[s], OpenSceneMode.Single);
            string scene = System.IO.Path.GetFileNameWithoutExtension(Scenes[s]);

            GameObject decor = GameObject.Find("Decor");
            if (decor == null)
            {
                sb.AppendLine(scene + ": SIN nodo Decor");
                continue;
            }

            Dictionary<string, int> tally = new Dictionary<string, int>();
            CountAll(decor.transform, tally);

            List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>(tally);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));

            int layers = 0;
            StringBuilder layerRow = new StringBuilder();
            for (int i = 0; i < decor.transform.childCount; i++)
            {
                Transform c = decor.transform.GetChild(i);
                if (PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject) != null) continue;
                if (c.name == "Path") continue;
                layers += c.childCount;
                layerRow.Append(c.name).Append("=").Append(c.childCount).Append(" ");
            }

            sb.AppendLine("### " + scene + "  total=" + Total(tally) + "  capas[" + layerRow.ToString().Trim() + "]");

            int shown = 0;
            for (int k = 0; k < list.Count && shown < 12; k++, shown++)
                sb.Append("   ").Append(list[k].Key).Append(" x").Append(list[k].Value);
            sb.AppendLine();

            sb.AppendLine("   plantas=" + Family(tally, PlantWords) + " aldea=" + Family(tally, VillageWords));
        }

        EditorSceneManager.OpenScene(Scenes[0], OpenSceneMode.Single);
        return sb.ToString();
    }

    static int Total(Dictionary<string, int> t)
    {
        int n = 0;
        foreach (KeyValuePair<string, int> kv in t) n += kv.Value;
        return n;
    }

    static int Family(Dictionary<string, int> t, string[] words)
    {
        int n = 0;
        foreach (KeyValuePair<string, int> kv in t)
        {
            string lower = kv.Key.ToLowerInvariant();
            for (int i = 0; i < words.Length; i++)
                if (lower.Contains(words[i])) { n += kv.Value; break; }
        }
        return n;
    }

    static void CountAll(Transform t, Dictionary<string, int> tally)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            Transform child = t.GetChild(i);
            GameObject src = PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);

            if (src != null)
            {
                string key = src.name;
                int dot = key.IndexOf('_');
                if (dot > 0) key = key.Substring(0, dot);
                if (tally.ContainsKey(key)) tally[key]++;
                else tally[key] = 1;
            }

            CountAll(child, tally);
        }
    }
}
