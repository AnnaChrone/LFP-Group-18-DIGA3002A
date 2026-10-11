using UnityEditor;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{

    [Header("UI Panels")]
    public GameObject PauseMenuPanel;
    public GameObject AudioSettingsPanel;
    public GameObject KeybindsPanel;

    [Header("Controller booleans")]
    [SerializeField] private bool AudioOpen =false;
    [SerializeField] private bool KeybindOpen = false;
    public PlayerControls playerControls;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PauseMenuPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
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

            //here can change the sprite of pause buttom

        }
        else
        {
            //disable
            PauseMenuPanel.SetActive(false);
            //here can change sprite of pause button
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
