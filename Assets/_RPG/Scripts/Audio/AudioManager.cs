using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Audio real del juego: pool de AudioSources para efectos, una fuente de
/// música y volumen general / música / efectos aplicado de verdad.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public SfxLibrary library;
    public int sfxVoices = 6;

    public float MasterVolume = 0.85f;
    public float MusicVolume = 0.55f;
    public float SfxVolume = 0.9f;

    private AudioSource musicSource;
    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private int nextVoice;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (library == null) library = Resources.Load<SfxLibrary>("SfxLibrary");

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        for (int i = 0; i < sfxVoices; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            sfxSources.Add(src);
        }

        if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        ApplyVolumes();
    }

    public void PlaySfx(SfxId id)
    {
        if (library == null) library = Resources.Load<SfxLibrary>("SfxLibrary");
        if (library == null) return;
        AudioClip clip = library.Get(id);
        if (clip == null) return;

        AudioSource src = sfxSources[nextVoice];
        nextVoice = (nextVoice + 1) % sfxSources.Count;
        src.clip = clip;
        src.volume = Mathf.Clamp01(MasterVolume * SfxVolume);
        src.pitch = 1f;
        src.Play();
    }

    public void PlayMusic()
    {
        if (library == null) library = Resources.Load<SfxLibrary>("SfxLibrary");
        if (library == null || library.music == null) return;
        if (musicSource.isPlaying && musicSource.clip == library.music) return;
        musicSource.clip = library.music;
        musicSource.volume = Mathf.Clamp01(MasterVolume * MusicVolume);
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void ApplyVolumes()
    {
        AudioListener.volume = MasterVolume;
        if (musicSource != null) musicSource.volume = MasterVolume * MusicVolume;
        for (int i = 0; i < sfxSources.Count; i++)
            sfxSources[i].volume = MasterVolume * SfxVolume;
    }
}
