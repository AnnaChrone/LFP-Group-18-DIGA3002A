using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{

    [Header("UI Panels and sprites")]
    public GameObject PauseMenuPanel;
    public GameObject AudioSettingsPanel;
    public GameObject KeybindsPanel;
    public Image PauseButtonSprite;
    public Sprite PausedSprite;
    public Sprite UnPausedSprite;

    [Header("Audio Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Volume Labels")]
    public TextMeshProUGUI masterLabel;
    public TextMeshProUGUI musicLabel;
    public TextMeshProUGUI sfxLabel;

    [Header("Controller booleans")]
    [SerializeField] private bool AudioOpen =false;
    [SerializeField] private bool KeybindOpen = false;
    public PlayerControls playerControls;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PauseMenuPanel.SetActive(false);
        SetupAudioSliders();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void SetupAudioSliders()
    {
        if (SoundManager.Instance == null) return;

        masterSlider.minValue = 0f; masterSlider.maxValue = 1f;
        musicSlider.minValue = 0f; musicSlider.maxValue = 1f;
        sfxSlider.minValue = 0f; sfxSlider.maxValue = 1f;

        masterSlider.value = SoundManager.Instance.masterVolume;
        musicSlider.value = SoundManager.Instance.musicVolume;
        sfxSlider.value = SoundManager.Instance.sfxVolume;

        // Initial label text
        UpdateLabel(masterLabel, masterSlider.value);
        UpdateLabel(musicLabel, musicSlider.value);
        UpdateLabel(sfxLabel, sfxSlider.value);

        // Slider -> SoundManager + label
        masterSlider.onValueChanged.AddListener(v =>
        {
            SoundManager.Instance.SetMasterVolume(v);
            UpdateLabel(masterLabel, v);
        });

        musicSlider.onValueChanged.AddListener(v =>
        {
            SoundManager.Instance.SetMusicVolume(v);
            UpdateLabel(musicLabel, v);
        });

        sfxSlider.onValueChanged.AddListener(v =>
        {
            SoundManager.Instance.SetSFXVolume(v);
            UpdateLabel(sfxLabel, v);
        });
    }

    void UpdateLabel(TMP_Text label, float value)
    {
        if (label == null) return;
        label.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    public void togglePauseMenu(bool paused)
    {
        if (paused)
        {
            //Enable
            PauseMenuPanel.SetActive(true);
            AudioSettingsPanel.SetActive(false);
            KeybindsPanel.SetActive(false);
            AudioOpen = false;
            KeybindOpen = false;
            Time.timeScale = 0f;
            PauseButtonSprite.sprite = PausedSprite;

        }
        else
        {
            //disable
            PauseMenuPanel.SetActive(false);
            PauseButtonSprite.sprite = UnPausedSprite;
            Time.timeScale = 1f;

        }
    }

    public void PauseButton()
    {
        playerControls.GamePaused = !playerControls.GamePaused;
        togglePauseMenu(playerControls.GamePaused);
    }

    public void toggleAudioSettings()
    {
        //here need to enable the AudioSettings panel, and then have the logic for the altering of the bars
        AudioSettingsPanel.SetActive(!AudioOpen);
        AudioOpen = !AudioOpen;


    }

    public void toggleKeybinds()
    {
        //Eventually, here we can make the player be able to set their own keybinds maybe
        KeybindsPanel.SetActive(!KeybindOpen);
        KeybindOpen = !KeybindOpen;
    }

    public void ExitGame()
    {
        //Are you sure? You will lose your game progress 

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
