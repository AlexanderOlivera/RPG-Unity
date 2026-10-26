using UnityEngine;

public enum SfxId
{
    Slash = 0,
    Hurt = 1,
    Item = 2,
    EnemyDeath = 3,
    Music = 4
}

/// <summary>Biblioteca de sonidos del proyecto (se carga desde Resources/SfxLibrary).</summary>
public class SfxLibrary : ScriptableObject
{
    public AudioClip slash;
    public AudioClip hurt;
    public AudioClip item;
    public AudioClip enemyDeath;
    public AudioClip music;

    public AudioClip Get(SfxId id)
    {
        switch (id)
        {
            case SfxId.Slash: return slash;
            case SfxId.Hurt: return hurt;
            case SfxId.Item: return item;
            case SfxId.EnemyDeath: return enemyDeath;
            case SfxId.Music: return music;
        }
        return null;
    }
}
