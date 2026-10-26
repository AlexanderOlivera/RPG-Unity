using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Sonda de comprobación: devuelve un resumen de lo generado (prefabs nuevos,
/// aldeanos animados, jefe con invocación, decoración por capas y créditos).
/// </summary>
public static class AuraSceneProbe
{
    public static string Execute()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("--- PREFABS ---");
        sb.AppendLine(SporeProbe());
        sb.AppendLine(BossProbe());
        sb.AppendLine(NpcProbe());
        sb.AppendLine(PlayerProbe());

        sb.AppendLine("--- ESCENAS ---");
        sb.AppendLine(Inspect("Assets/_RPG/Scenes/01_Bosque.unity"));
        sb.AppendLine(Inspect("Assets/_RPG/Scenes/03_Cuevas.unity"));
        sb.AppendLine(Inspect("Assets/_RPG/Scenes/05_Santuario.unity"));

        sb.AppendLine("--- EXTRAS ---");
        sb.Append("Creditos=").Append(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_RPG/Scenes/06_Creditos.unity") != null);
        sb.Append(" | EscenasEnBuild=").Append(EditorBuildSettings.scenes.Length);
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            sb.Append("\n  ").Append(i).Append(": ").Append(EditorBuildSettings.scenes[i].path);

        return sb.ToString();
    }

    static string SporeProbe()
    {
        GameObject spore = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_RPG/Prefabs/Enemy_Spore.prefab");
        if (spore == null) return "Enemy_Spore: FALTA";

        Health h = spore.GetComponent<Health>();
        SpriteRenderer sr = spore.GetComponent<SpriteRenderer>();
        return "Enemy_Spore: vida=" + (h != null ? h.maxHealth.ToString() : "?") +
               " burst=" + (spore.GetComponent<SporeBurst>() != null) +
               " color=" + (sr != null ? ColorText(sr.color) : "?") +
               " scale=" + spore.transform.localScale.x.ToString("F2");
    }

    static string BossProbe()
    {
        GameObject boss = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_RPG/Prefabs/Boss_Guardian.prefab");
        if (boss == null) return "Boss_Guardian: FALTA";

        BossSummoner summoner = boss.GetComponent<BossSummoner>();
        Health h = boss.GetComponent<Health>();
        if (summoner == null) return "Boss_Guardian: sin BossSummoner";

        return "Boss_Guardian: vida=" + (h != null ? h.maxHealth.ToString() : "?") +
               " invoca=" + (summoner.summonPrefab != null ? summoner.summonPrefab.name : "NULL") +
               " oleada=" + summoner.perWave + " cada=" + summoner.interval + "s max=" + summoner.maxAlive;
    }

    static string NpcProbe()
    {
        GameObject npc = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_RPG/Prefabs/NPC.prefab");
        if (npc == null) return "NPC: FALTA";

        BoxCollider2D col = npc.GetComponent<BoxCollider2D>();
        Rigidbody2D rb = npc.GetComponent<Rigidbody2D>();
        NpcAnimator anim = npc.GetComponent<NpcAnimator>();
        NpcWanderer wanderer = npc.GetComponent<NpcWanderer>();
        Transform shadow = npc.transform.Find("Shadow");

        return "NPC: wanderer=" + (wanderer != null) +
               " velocidad=" + (wanderer != null ? wanderer.walkSpeed.ToString("F2") : "?") +
               " animador=" + (anim != null) +
               " framesFrente=" + (anim != null && anim.walkDown != null ? anim.walkDown.Length : 0) +
               " framesEspalda=" + (anim != null && anim.walkUp != null ? anim.walkUp.Length : 0) +
               " framesPerfil=" + (anim != null && anim.walkSide != null ? anim.walkSide.Length : 0) +
               " sombra=" + (shadow != null) +
               " trigger=" + (col != null && col.isTrigger) +
               " rb=" + (rb != null ? rb.bodyType.ToString() : "sin");
    }

    static string PlayerProbe()
    {
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_RPG/Prefabs/Player.prefab");
        if (player == null) return "Player: FALTA";

        Health h = player.GetComponent<Health>();
        return "Player: vida=" + (h != null ? h.maxHealth.ToString() : "?") +
               " sombra=" + (player.transform.Find("Shadow") != null);
    }

    static string Inspect(string path)
    {
        EditorSceneManager.OpenScene(path);
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();

        StringBuilder sb = new StringBuilder();
        sb.Append(path).Append(" | ");

        GameObject[] roots = scene.GetRootGameObjects();
        int decor = 0, dosel = 0, maleza = 0, cortina = 0, luciernagas = 0, npcs = 0, enemigos = 0, items = 0;
        string floorSize = "?";

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject go = roots[i];
            if (go.name == "NPCs") npcs = go.transform.childCount;
            else if (go.name == "Enemies") enemigos = go.transform.childCount;
            else if (go.name == "Collectibles") items = go.transform.childCount;
            else if (go.name == "Floor")
            {
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) floorSize = sr.size.x + "x" + sr.size.y;
            }
            else if (go.name == "Decor")
            {
                // Las capas cuelgan de Decor, no de la raíz de la escena.
                decor = go.transform.childCount;
                for (int c = 0; c < go.transform.childCount; c++)
                {
                    Transform child = go.transform.GetChild(c);
                    if (child.name == "Dosel") dosel = child.childCount;
                    else if (child.name == "Maleza") maleza = child.childCount;
                    else if (child.name == "Cortina") cortina = child.childCount;
                    else if (child.name == "Luciernagas") luciernagas = child.childCount;
                }
            }
        }

        int props = Mathf.Max(0, decor - (dosel > 0 ? 1 : 0) - (maleza > 0 ? 1 : 0) -
                                  (cortina > 0 ? 1 : 0) - (luciernagas > 0 ? 1 : 0) - 1);

        sb.Append("suelo=").Append(floorSize)
          .Append(" props=").Append(props)
          .Append(" dosel=").Append(dosel)
          .Append(" maleza=").Append(maleza)
          .Append(" cortina=").Append(cortina)
          .Append(" luciernagas=").Append(luciernagas)
          .Append(" npcs=").Append(npcs)
          .Append(" enemigos=").Append(enemigos)
          .Append(" objetos=").Append(items);

        return sb.ToString();
    }

    static string ColorText(Color c)
    {
        return c.r.ToString("F2") + "/" + c.g.ToString("F2") + "/" + c.b.ToString("F2");
    }
}
