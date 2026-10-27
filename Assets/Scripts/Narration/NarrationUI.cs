using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Two text fields and a Continue button.
public class NarrationUI : MonoBehaviour
{
    [SerializeField] private GameObject root;        // the whole narration box
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button continueButton;

    [Header("Typewriter")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float charsPerSecond = 40f;

    // Fired when the player presses Continue (after the line has fully appeared).
    public event Action Continued;

    private Coroutine typing;
    private bool continuePressed;
    private bool waitingForContinue;

    private void Awake()
    {
        continueButton.onClick.AddListener(Advance);
        continueButton.gameObject.SetActive(false);
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(Advance);
    }

    // Shows a line. Returns how long (seconds) the text takes to finish appearing.
    public float Show(string speaker, string text)
    {
        root.SetActive(true);
        continuePressed = false;
        waitingForContinue = true;
        continueButton.gameObject.SetActive(false);

        bool hasSpeaker = !string.IsNullOrEmpty(speaker) && speaker.ToLower() != "narrator";
        speakerText.gameObject.SetActive(hasSpeaker);
        if (hasSpeaker) speakerText.text = speaker;

        bodyText.text = text;

        if (typing != null) StopCoroutine(typing);
        typing = null;

        if (!useTypewriter)
        {
            bodyText.maxVisibleCharacters = int.MaxValue;
            continueButton.gameObject.SetActive(true);
            return 0f;
        }

        typing = StartCoroutine(Type(text.Length));
        return text.Length / Mathf.Max(1f, charsPerSecond);
    }

    // Shows a line and waits until the player presses Continue.
    // Usage from another coroutine: yield return narration.ShowAndWait("Narrator", "Hello.");
    public IEnumerator ShowAndWait(string speaker, string text)
    {
        Show(speaker, text);
        while (!continuePressed)
            yield return null;
    }

    // Wired to the Continue button. Also public so you can call it from a key/click handler.
    // If the text is still typing, the first call finishes the line instantly;
    // the next call continues.
    public void Advance()
    {
        if (typing != null)
        {
            StopCoroutine(typing);
            typing = null;
            bodyText.maxVisibleCharacters = int.MaxValue;
            continueButton.gameObject.SetActive(true);
            return;
        }

        continuePressed = true;
        waitingForContinue = false;
        Continued?.Invoke();
    }

    // Ignored while a line is waiting for Continue, so nothing else can close
    // the panel early. Pass true to force it closed (e.g. skipping a cutscene).
    // The warning in the Console shows which script made the call.
    public void Hide(bool force = false)
    {
        if (!force && waitingForContinue && root.activeSelf)
        {
            Debug.LogWarning("[NarrationUI] Hide() ignored: still waiting for Continue.", this);
            return;
        }

        if (typing != null) StopCoroutine(typing);
        typing = null;
        waitingForContinue = false;
        continueButton.gameObject.SetActive(false);
        root.SetActive(false);
    }

    private IEnumerator Type(int length)
    {
        float delay = 1f / Mathf.Max(1f, charsPerSecond);
        for (int i = 0; i <= length; i++)
        {
            bodyText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }
        bodyText.maxVisibleCharacters = int.MaxValue;
        typing = null;
        continueButton.gameObject.SetActive(true);
    }
}