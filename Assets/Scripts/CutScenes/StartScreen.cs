using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Start screen that lives in the same scene as the game.
/// - Holds the game still (time frozen) until Start is pressed.
/// - Start hides the panel and plays the first cutscene.
/// - Quit closes the game (or stops Play mode in the editor).
/// Put this on the StartPanel object.
/// </summary>
public class StartScreen : MonoBehaviour
{
    [Header("References")]
    public GameObject startPanel;
    public Button startButton;
    public Button quitButton;

    [Header("First Cutscene")]
    [Tooltip("Plays right after Start is pressed (your Panel01 intro asset).")]
    public CutsceneData introCutscene;

    [Header("Hold The Game Until Start Is Pressed")]
    public bool freezeTimeUntilStart = true;
    [Tooltip("Optional: scripts to switch off until Start is pressed (e.g. player movement).")]
    public Behaviour[] disableUntilStart;

    private float timeScaleBefore = 1f;
    private bool holding = false;
    private bool started = false;

    private void Awake()
    {
        if (startPanel == null) startPanel = gameObject;

        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void Start()
    {
        startPanel.SetActive(true);
        HoldGame();
    }

    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);

        // Never leave the game frozen if this object disappears.
        ReleaseGame();
    }

    private void HoldGame()
    {
        if (holding) return;
        holding = true;

        if (freezeTimeUntilStart)
        {
            timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (disableUntilStart != null)
        {
            foreach (Behaviour b in disableUntilStart)
                if (b != null) b.enabled = false;
        }
    }

    private void ReleaseGame()
    {
        if (!holding) return;
        holding = false;

        if (freezeTimeUntilStart)
            Time.timeScale = Mathf.Approximately(timeScaleBefore, 0f) ? 1f : timeScaleBefore;

        if (disableUntilStart != null)
        {
            foreach (Behaviour b in disableUntilStart)
                if (b != null) b.enabled = true;
        }
    }

    public void OnStartClicked()
    {
        if (started) return;
        started = true;

        // Release first so the cutscene saves the NORMAL time scale (not 0)
        // and restores it correctly when it ends.
        ReleaseGame();
        startPanel.SetActive(false);

        if (introCutscene == null)
        {
            Debug.LogWarning("[StartScreen] No intro cutscene assigned - starting the game without one.");
            return;
        }

        if (CutsceneController.Instance == null)
        {
            Debug.LogWarning("[StartScreen] No CutsceneController found - starting the game without the intro.");
            return;
        }

        CutsceneController.Instance.PlayCutscene(introCutscene);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}