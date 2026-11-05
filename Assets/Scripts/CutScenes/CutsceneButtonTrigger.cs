using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on any UI Button that should start a cutscene when clicked.
/// It runs alongside whatever the button already does (e.g. changing spawn points).
/// Looks up CutsceneController.Instance at click time, so it works across scenes.
/// </summary>
[RequireComponent(typeof(Button))]
public class CutsceneButtonTrigger : MonoBehaviour
{
    [Tooltip("The cutscene (CutsceneData asset) to play when this button is clicked.")]
    public CutsceneData cutscene;

    [Tooltip("Seconds to wait before the cutscene starts, so the button's existing logic (spawn point changes etc.) can finish first.")]
    public float delay = 0f;

    [Tooltip("If true, this button only ever triggers its cutscene once.")]
    public bool playOnce = true;

    private bool hasPlayed = false;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Play);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(Play);
    }

    public void Play()
    {
        if (playOnce && hasPlayed) return;

        if (CutsceneController.Instance == null)
        {
            Debug.LogWarning("[CutsceneButtonTrigger] No CutsceneController in the scene!");
            return;
        }

        if (cutscene == null)
        {
            Debug.LogWarning($"[CutsceneButtonTrigger] No cutscene assigned on {name}");
            return;
        }

        hasPlayed = true;

        if (delay <= 0f)
        {
            CutsceneController.Instance.PlayCutscene(cutscene);
        }
        else
        {
            // Run on the controller so the delay survives this button being hidden/disabled.
            CutsceneController.Instance.StartCoroutine(PlayAfterDelay());
        }
    }

    private IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delay);
        CutsceneController.Instance.PlayCutscene(cutscene);
    }
}