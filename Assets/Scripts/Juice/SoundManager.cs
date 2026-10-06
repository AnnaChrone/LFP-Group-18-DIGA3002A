using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central sound manager: music, ambience, footsteps and "juice" SFX.
/// Add to an empty GameObject in your first scene. Assign clips in the Inspector.
/// Call from anywhere:  SoundManager.Instance.PlayCoin();
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // ---------------------------------------------------------------- Volumes
    [Header("Volumes")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float ambienceVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    // ------------------------------------------------------- Music & Ambience
    [Header("Music & Ambience")]
    public AudioClip[] musicTracks;
    public AudioClip ambienceLoop;           // wind, birds, water, etc.
    public bool playMusicOnStart = true;
    public bool playAmbienceOnStart = true;
    public float fadeTime = 1.5f;

    // -------------------------------------------------------------- Footsteps
    [System.Serializable]
    public class SurfaceFootsteps
    {
        public string surfaceName = "Default";   // match this to a tag or name you pass in
        public AudioClip[] clips;
    }

    [Header("Footsteps")]
    public SurfaceFootsteps[] surfaces;           // first entry is the fallback
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.3f;
    [Range(0f, 1f)] public float footstepVolume = 0.6f;

    // ------------------------------------------------------------- Juice SFX
    [Header("Coins")]
    public AudioClip[] coinClips;
    public float coinComboWindow = 1f;            // time to keep the combo alive
    public float coinPitchStep = 0.06f;           // pitch rise per coin in a combo
    public float coinMaxPitch = 1.8f;

    [Header("Fishing")]
    public AudioClip castClip;
    public AudioClip splashClip;
    public AudioClip biteClip;
    public AudioClip[] catchClips;
    public AudioClip rareCatchClip;

    [Header("Spawn / Misc")]
    public AudioClip[] spawnClips;
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip uiClickClip;
    public AudioClip levelUpClip;

    [Header("Pool")]
    public int sfxPoolSize = 12;

    // --------------------------------------------------------------- Internals
    AudioSource musicSource;
    AudioSource ambienceSource;
    readonly List<AudioSource> sfxPool = new List<AudioSource>();
    int poolIndex;

    float stepTimer;
    int coinCombo;
    float lastCoinTime = -999f;
    int lastMusicIndex = -1;
    Coroutine musicRoutine;

    // ----------------------------------------------------------------- Setup
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();

        musicSource = CreateSource("Music", true);
        ambienceSource = CreateSource("Ambience", true);

        for (int i = 0; i < sfxPoolSize; i++)
            sfxPool.Add(CreateSource("SFX_" + i, false));
    }

    void Start()
    {
        if (playMusicOnStart) PlayRandomMusic();
        if (playAmbienceOnStart) PlayAmbience(ambienceLoop);
    }

    AudioSource CreateSource(string objName, bool loop)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f; // 2D by default
        return src;
    }

    // ---------------------------------------------------------- Core SFX call
    /// <summary>Plays a one-shot with optional pitch/volume randomness.</summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f,
                        float pitchVariance = 0f, Vector3? worldPos = null)
    {
        if (clip == null) return;

        var src = GetFreeSource();
        src.transform.position = worldPos ?? transform.position;
        src.spatialBlend = worldPos.HasValue ? 1f : 0f;
        src.pitch = pitch + Random.Range(-pitchVariance, pitchVariance);
        src.volume = volume * sfxVolume * masterVolume;
        src.clip = clip;
        src.Play();
    }

    public void PlayRandomSFX(AudioClip[] clips, float volume = 1f, float pitch = 1f,
                              float pitchVariance = 0f, Vector3? worldPos = null)
    {
        if (clips == null || clips.Length == 0) return;
        PlaySFX(clips[Random.Range(0, clips.Length)], volume, pitch, pitchVariance, worldPos);
    }

    AudioSource GetFreeSource()
    {
        // Prefer an idle source, otherwise steal the next one round-robin.
        for (int i = 0; i < sfxPool.Count; i++)
        {
            var s = sfxPool[(poolIndex + i) % sfxPool.Count];
            if (!s.isPlaying)
            {
                poolIndex = (poolIndex + i + 1) % sfxPool.Count;
                return s;
            }
        }
        var stolen = sfxPool[poolIndex];
        poolIndex = (poolIndex + 1) % sfxPool.Count;
        return stolen;
    }

    // ------------------------------------------------------------ Footsteps
    /// <summary>
    /// Call every frame from your player controller.
    /// Example: SoundManager.Instance.UpdateFootsteps(isGrounded && moving, isSprinting, surfaceName);
    /// </summary>
    public void UpdateFootsteps(bool isMoving, bool isRunning = false, string surface = "Default")
    {
        if (!isMoving) { stepTimer = 0f; return; }

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayFootstep(surface);
            stepTimer = isRunning ? runStepInterval : walkStepInterval;
        }
    }

    public void PlayFootstep(string surface = "Default")
    {
        AudioClip[] clips = null;

        if (surfaces != null && surfaces.Length > 0)
        {
            clips = surfaces[0].clips; // fallback
            foreach (var s in surfaces)
                if (s.surfaceName == surface) { clips = s.clips; break; }
        }

        PlayRandomSFX(clips, footstepVolume, 1f, 0.1f);
    }

    // ---------------------------------------------------------------- Coins
    /// <summary>Pitch climbs as the player grabs coins in quick succession.</summary>
    public void PlayCoin()
    {
        if (Time.time - lastCoinTime > coinComboWindow) coinCombo = 0;
        lastCoinTime = Time.time;

        float pitch = Mathf.Min(1f + coinCombo * coinPitchStep, coinMaxPitch);
        coinCombo++;

        PlayRandomSFX(coinClips, 0.9f, pitch, 0.02f);
    }

    // -------------------------------------------------------------- Fishing
    public void PlayCast() => PlaySFX(castClip, 0.8f, 1f, 0.05f);
    public void PlaySplash() => PlaySFX(splashClip, 0.9f, 1f, 0.1f);
    public void PlayBite() => PlaySFX(biteClip, 1f, 1f, 0.03f);

    public void PlayFishCaught(bool rare = false)
    {
        if (rare && rareCatchClip != null)
        {
            PlaySFX(rareCatchClip, 1f);
            // little sparkle layered on top
            PlayRandomSFX(coinClips, 0.7f, 1.4f);
        }
        else
        {
            PlayRandomSFX(catchClips, 1f, 1f, 0.05f);
        }
    }

    // ----------------------------------------------------------- Spawn & misc
    public void PlaySpawn(Vector3? worldPos = null) =>
        PlayRandomSFX(spawnClips, 0.9f, 1f, 0.08f, worldPos);

    public void PlayJump() => PlaySFX(jumpClip, 0.7f, 1f, 0.05f);
    public void PlayLand() => PlaySFX(landClip, 0.8f, 1f, 0.1f);
    public void PlayUIClick() => PlaySFX(uiClickClip, 0.6f, 1f, 0.03f);
    public void PlayLevelUp() => PlaySFX(levelUpClip, 1f);

    // ---------------------------------------------------------------- Music
    public void PlayRandomMusic()
    {
        if (musicTracks == null || musicTracks.Length == 0) return;

        int idx;
        do { idx = Random.Range(0, musicTracks.Length); }
        while (musicTracks.Length > 1 && idx == lastMusicIndex);

        lastMusicIndex = idx;
        PlayMusic(musicTracks[idx]);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(SwapTrack(musicSource, clip, () => musicVolume));
    }

    public void PlayAmbience(AudioClip clip)
    {
        if (clip == null) return;
        StartCoroutine(SwapTrack(ambienceSource, clip, () => ambienceVolume));
    }

    public void StopMusic() => StartCoroutine(FadeOut(musicSource));
    public void StopAmbience() => StartCoroutine(FadeOut(ambienceSource));

    IEnumerator SwapTrack(AudioSource src, AudioClip clip, System.Func<float> targetVol)
    {
        // fade out current
        if (src.isPlaying) yield return FadeOut(src);

        src.clip = clip;
        src.volume = 0f;
        src.Play();

        // fade in
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(0f, targetVol() * masterVolume, t / fadeTime);
            yield return null;
        }
        src.volume = targetVol() * masterVolume;
    }

    IEnumerator FadeOut(AudioSource src)
    {
        float start = src.volume;
        float t = 0f;
        while (t < fadeTime * 0.5f)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, 0f, t / (fadeTime * 0.5f));
            yield return null;
        }
        src.Stop();
    }

    void Update()
    {
        // Auto-advance to the next track when the current one ends.
        if (musicSource != null && musicTracks != null && musicTracks.Length > 1 &&
            musicRoutine != null && !musicSource.isPlaying && musicSource.clip != null)
        {
            PlayRandomMusic();
        }
    }

    // ----------------------------------------------------- Volume settings
    public void SetMasterVolume(float v) { masterVolume = v; ApplyVolumes(); }
    public void SetMusicVolume(float v) { musicVolume = v; ApplyVolumes(); }
    public void SetAmbienceVolume(float v) { ambienceVolume = v; ApplyVolumes(); }
    public void SetSFXVolume(float v) { sfxVolume = v; ApplyVolumes(); }

    void ApplyVolumes()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.volume = musicVolume * masterVolume;
        if (ambienceSource != null && ambienceSource.isPlaying)
            ambienceSource.volume = ambienceVolume * masterVolume;

        PlayerPrefs.SetFloat("vol_master", masterVolume);
        PlayerPrefs.SetFloat("vol_music", musicVolume);
        PlayerPrefs.SetFloat("vol_ambience", ambienceVolume);
        PlayerPrefs.SetFloat("vol_sfx", sfxVolume);
    }

    void LoadVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat("vol_master", masterVolume);
        musicVolume = PlayerPrefs.GetFloat("vol_music", musicVolume);
        ambienceVolume = PlayerPrefs.GetFloat("vol_ambience", ambienceVolume);
        sfxVolume = PlayerPrefs.GetFloat("vol_sfx", sfxVolume);
    }
}