using System.Collections;
using UnityEngine;

/// <summary>
/// Simple sound manager: one slot per sound, assign each clip in the Inspector.
/// Add to an empty GameObject in your first scene.
/// Call from anywhere, e.g.  SoundManager.Instance.PlayCatch();
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // ---------------------------------------------------------------- Volumes
    [Header("Volumes")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    // ------------------------------------------------------------- Background
    [Header("Background Music")]
    public AudioClip backgroundMusic;
    public bool playMusicOnStart = true;
    public float fadeTime = 1.5f;

    // ---------------------------------------------------------------- Fishing
    [Header("Fishing Sounds")]
    public AudioClip castSound;     // rod is cast
    public AudioClip splashSound;   // rod lands in the water
    public AudioClip reelSound;     // reeling in
    public AudioClip catchSound;    // fish caught

    // ------------------------------------------------- Player / UI / Restaurant
    [Header("Other Sounds")]
    public AudioClip walkSound;     // loops while the player is moving
    public AudioClip clickSound;    // UI click / open / close
    public AudioClip coinSound;     // coin earned in the restaurant

    // --------------------------------------------------------------- Internals
    AudioSource musicSource;
    AudioSource sfxSource;
    AudioSource walkSource;
    AudioSource reelSource;
    Coroutine musicRoutine;

    float SfxVol => sfxVolume * masterVolume;

    // ------------------------------------------------------------------ Setup
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();

        musicSource = CreateSource("Music", true);
        sfxSource = CreateSource("SFX", false);
        walkSource = CreateSource("Walk", true);
        reelSource = CreateSource("Reel", true);
    }

    void Start()
    {
        if (playMusicOnStart) PlayMusic(backgroundMusic);
    }

    AudioSource CreateSource(string objName, bool loop)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f; // 2D
        return src;
    }

    void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume * SfxVol);
    }

    // ---------------------------------------------------------------- Fishing
    public void PlayCast() => PlayOneShot(castSound);
    public void PlaySplash() => PlayOneShot(splashSound);
    public void PlayCatch() => PlayOneShot(catchSound);

    // Kept so older calls like PlayFishCaught() still work.
    public void PlayFishCaught(bool rare = false) => PlayCatch();

    /// <summary>One-shot reel sound.</summary>
    public void PlayReel() => PlayOneShot(reelSound);

    /// <summary>Looping reel sound: call Start when reeling begins, Stop when it ends.</summary>
    public void StartReel()
    {
        if (reelSound == null) return;
        if (!reelSource.isPlaying)
        {
            reelSource.clip = reelSound;
            reelSource.volume = SfxVol;
            reelSource.Play();
        }
    }

    public void StopReel()
    {
        if (reelSource.isPlaying) reelSource.Stop();
    }

    // ---------------------------------------------------------------- UI & coin
    public void PlayClick() => PlayOneShot(clickSound);
    public void PlayUIClick() => PlayClick();   // kept for older calls

    public void PlayCoin() => PlayOneShot(coinSound);

    // ---------------------------------------------------------------- Walking
    /// <summary>
    /// Call every frame from the player controller:
    /// SoundManager.Instance.SetWalking(moveInput != Vector2.zero);
    /// The sound loops while true and stops when false.
    /// </summary>
    public void SetWalking(bool isWalking)
    {
        if (walkSound == null) return;

        if (isWalking)
        {
            if (!walkSource.isPlaying)
            {
                walkSource.clip = walkSound;
                walkSource.volume = SfxVol;
                walkSource.Play();
            }
        }
        else if (walkSource.isPlaying)
        {
            walkSource.Stop();
        }
    }

    // Kept so older calls like UpdateFootsteps(moving, running, surface) still work.
    public void UpdateFootsteps(bool isMoving, bool isRunning = false, string surface = "Default")
    {
        SetWalking(isMoving);
    }

    // ------------------------------------------------------------------ Music
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(SwapTrack(clip));
    }

    public void StopMusic()
    {
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(FadeOut());
    }

    IEnumerator SwapTrack(AudioClip clip)
    {
        if (musicSource.isPlaying) yield return FadeOut();

        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume * masterVolume, t / fadeTime);
            yield return null;
        }
        musicSource.volume = musicVolume * masterVolume;
    }

    IEnumerator FadeOut()
    {
        float start = musicSource.volume;
        float half = Mathf.Max(0.01f, fadeTime * 0.5f);
        float t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, 0f, t / half);
            yield return null;
        }
        musicSource.Stop();
    }

    // -------------------------------------------------------- Volume settings
    public void SetMasterVolume(float v) { masterVolume = v; ApplyVolumes(); SaveVolumes(); }
    public void SetMusicVolume(float v) { musicVolume = v; ApplyVolumes(); SaveVolumes(); }
    public void SetSFXVolume(float v) { sfxVolume = v; ApplyVolumes(); SaveVolumes(); }

    void ApplyVolumes()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.volume = musicVolume * masterVolume;
        if (walkSource != null && walkSource.isPlaying)
            walkSource.volume = SfxVol;
        if (reelSource != null && reelSource.isPlaying)
            reelSource.volume = SfxVol;
    }

    void SaveVolumes()
    {
        PlayerPrefs.SetFloat("Vol_Master", masterVolume);
        PlayerPrefs.SetFloat("Vol_Music", musicVolume);
        PlayerPrefs.SetFloat("Vol_SFX", sfxVolume);
    }

    void LoadVolumes()
    {
        // Falls back to the Inspector values if nothing has been saved yet.
        masterVolume = PlayerPrefs.GetFloat("Vol_Master", masterVolume);
        musicVolume = PlayerPrefs.GetFloat("Vol_Music", musicVolume);
        sfxVolume = PlayerPrefs.GetFloat("Vol_SFX", sfxVolume);
    }
}