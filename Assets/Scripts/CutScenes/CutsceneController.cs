using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class CutsceneController : MonoBehaviour
{
    public static CutsceneController Instance { get; private set; }

    [Header("References")]
    public GameObject cutscenePanel;
    public Image backgroundImage;
    public TMP_Text cutsceneText;
    [Tooltip("Optional: your 'Press [SPACE] to continue' text. It is placed along the bottom of each cutscene's text area.")]
    public TMP_Text promptText;
    public Image graphicImage;          // optional
    public AudioSource cutsceneAudio;   // optional

    [Header("Settings")]
    public float textFadeInTime = 0.5f;
    public KeyCode continueKey = KeyCode.Space;

    [Header("Freeze Player While Playing")]
    [Tooltip("Drag your player movement script(s) here. They are disabled during a cutscene and re-enabled after.")]
    public Behaviour[] disableWhilePlaying;
    public UnityEvent onAnyCutsceneStart;
    public UnityEvent onAnyCutsceneEnd;

    [Header("Cutscenes (optional - buttons can hold their own CutsceneData)")]
    public CutsceneData IntroCutscene;
    public CutsceneData Act1Cutscene;
    public CutsceneData Act2Cutscene;
    public CutsceneData CombatEndingCutscene;
    public CutsceneData CozyEndingCutscene;

    [Header("Teleport Settings (player must have the 'Player' tag)")]
    public Vector2 act2TeleportPosition = new Vector2(0, 0);
    public string act2TeleportSceneName = "";
    public GameObject teleportTargetObject;

    [Header("Persistence")]
    [Tooltip("Only tick this if the controller sits on a ROOT object and you load new scenes.")]
    public bool persistAcrossScenes = false;

    // State
    private CutsceneData currentCutscene;
    private int currentStepIndex = 0;
    private bool isWaitingForInput = false;
    private bool isTypingLine = false;
    private bool skipFade = false;
    private Action currentOnComplete;
    private Coroutine currentCoroutine;
    private readonly List<string> accumulatedLines = new List<string>();
    private readonly List<Behaviour> disabledByUs = new List<Behaviour>();

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (persistAcrossScenes)
                DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this); // removes only this script, never the object it sits on
            return;
        }

        if (cutscenePanel != null)
            cutscenePanel.SetActive(false);
    }

    private void Update()
    {
        if (!IsPlaying) return;

        // Only show the prompt when pressing Space will actually do something
        if (promptText != null)
            promptText.enabled = isWaitingForInput && !isTypingLine;

        if (ContinuePressed())
        {
            if (isTypingLine)
                skipFade = true;             // first press: finish the fade instantly
            else if (isWaitingForInput)
                AdvanceToNextStep();         // next press: continue
        }
    }

    // Works with the old Input Manager, the new Input System, or both.
    private bool ContinuePressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Keyboard kb = Keyboard.current;
        return kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(continueKey);
#endif
    }

    // ------------------------------------------------------------------
    // Playing
    // ------------------------------------------------------------------

    public void PlayCutscene(CutsceneData cutscene, Action onComplete = null)
    {
        if (cutscene == null)
        {
            Debug.LogWarning("[CutsceneController] Attempted to play null cutscene!");
            onComplete?.Invoke();
            return;
        }

        if (IsPlaying)
        {
            Debug.LogWarning("[CutsceneController] A cutscene is already playing - ignoring request.");
            return;
        }

        if (cutscenePanel == null || cutsceneText == null)
        {
            Debug.LogError("[CutsceneController] Cutscene Panel or Cutscene Text slot is empty on the controller!");
            return;
        }

        if (cutscene.steps == null || cutscene.steps.Length == 0)
            Debug.LogWarning($"[CutsceneController] Cutscene '{cutscene.name}' has NO STEPS - it will flash and end instantly. Add TextLine steps to the asset.");

        Debug.Log($"[CutsceneController] Playing '{cutscene.name}' ({(cutscene.steps != null ? cutscene.steps.Length : 0)} steps)");

        StopAllCoroutines();
        IsPlaying = true;

        FreezePlayer();
        onAnyCutsceneStart?.Invoke();

        currentCutscene = cutscene;
        currentOnComplete = onComplete;
        currentStepIndex = 0;
        accumulatedLines.Clear();
        isWaitingForInput = false;
        isTypingLine = false;
        skipFade = false;
        cutscenePanel.SetActive(true);

        cutsceneText.text = "";
        cutsceneText.color = new Color(cutsceneText.color.r, cutsceneText.color.g, cutsceneText.color.b, 1);

        if (graphicImage != null)
        {
            graphicImage.gameObject.SetActive(false);
            graphicImage.color = Color.white;
        }

        if (backgroundImage != null && cutscene.backgroundSprite != null)
            backgroundImage.sprite = cutscene.backgroundSprite;

        ApplyTextLayout(cutscene);

        ProcessCurrentStep();
    }

    [Header("Text Layout")]
    public float autoSizeMin = 18f;
    public float autoSizeMax = 40f;
    [Tooltip("How much of the screen height (0-1) the 'Press SPACE' strip takes at the bottom of the text area.")]
    public float promptStripHeight = 0.07f;

    private static void SetRect(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Puts the text (and the prompt) inside this cutscene's speech bubble.
    private void ApplyTextLayout(CutsceneData cutscene)
    {
        Vector2 min = cutscene.textAreaMin;
        Vector2 max = cutscene.textAreaMax;
        float strip = promptText != null ? promptStripHeight : 0f;

        cutsceneText.alignment = TextAlignmentOptions.Center;   // centered both ways
        cutsceneText.enableAutoSizing = true;
        cutsceneText.fontSizeMin = autoSizeMin;
        cutsceneText.fontSizeMax = autoSizeMax;
        SetRect(cutsceneText.rectTransform, new Vector2(min.x, min.y + strip), max);

        if (promptText != null)
        {
            promptText.alignment = TextAlignmentOptions.Center;
            SetRect(promptText.rectTransform, min, new Vector2(max.x, min.y + strip));
        }
    }

    public void PlayCombatEnding(Action onComplete = null)
    {
        PlayCutscene(CombatEndingCutscene, onComplete);
    }

    public void PlayCozyEnding(Action onComplete = null)
    {
        PlayCutscene(CozyEndingCutscene, onComplete);
    }

    // ------------------------------------------------------------------
    // Freeze / unfreeze player (no dependency on any player class)
    // ------------------------------------------------------------------

    private void FreezePlayer()
    {
        disabledByUs.Clear();
        if (disableWhilePlaying == null) return;

        foreach (Behaviour b in disableWhilePlaying)
        {
            if (b != null && b.enabled)
            {
                b.enabled = false;
                disabledByUs.Add(b);
            }
        }
    }

    private void UnfreezePlayer()
    {
        foreach (Behaviour b in disabledByUs)
        {
            if (b != null)
                b.enabled = true;
        }
        disabledByUs.Clear();
    }

    // ------------------------------------------------------------------
    // Step processing
    // ------------------------------------------------------------------

    private void ProcessCurrentStep()
    {
        if (currentCutscene.steps == null || currentStepIndex >= currentCutscene.steps.Length)
        {
            EndCutscene();
            return;
        }

        CutsceneStep step = currentCutscene.steps[currentStepIndex];

        switch (step.type)
        {
            case CutsceneStepType.TextLine:
                if (step.audioToPlay != null)
                    PlayAudio(step.audioToPlay);
                currentCoroutine = StartCoroutine(AddTextLine(step.text, step.textFadeTime > 0 ? step.textFadeTime : textFadeInTime));
                break;

            case CutsceneStepType.ClearText:
                ClearText();
                AdvanceToNextStep();
                break;

            case CutsceneStepType.ShowGraphic:
                if (step.audioToPlay != null)
                    PlayAudio(step.audioToPlay);
                ShowGraphic(step.graphicToShow, step.graphicFadeTime, step.clearTextBeforeGraphic);
                break;

            case CutsceneStepType.HideGraphic:
                HideGraphic(step.graphicFadeTime);
                break;

            case CutsceneStepType.WaitForInput:
                // TextLine and ShowGraphic already wait for the continue key,
                // so don't make the player press it twice in a row.
                if (PreviousStepAlreadyWaited())
                    AdvanceToNextStep();
                else
                    isWaitingForInput = true;
                break;

            case CutsceneStepType.TriggerEvent:
                step.onStepTrigger?.Invoke();
                AdvanceToNextStep();
                break;

            case CutsceneStepType.CustomDelay:
                currentCoroutine = StartCoroutine(DelayThenAdvance(step.delayTime));
                break;

            case CutsceneStepType.TeleportPlayer:
                TeleportPlayer(step.teleportPosition, step.teleportSceneName);
                AdvanceToNextStep();
                break;
        }
    }

    private bool PreviousStepAlreadyWaited()
    {
        if (currentStepIndex <= 0) return false;
        CutsceneStepType prev = currentCutscene.steps[currentStepIndex - 1].type;
        return prev == CutsceneStepType.TextLine || prev == CutsceneStepType.ShowGraphic;
    }

    private void AdvanceToNextStep()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        isWaitingForInput = false;
        isTypingLine = false;
        skipFade = false;
        currentStepIndex++;
        ProcessCurrentStep();
    }

    // ------------------------------------------------------------------
    // Text
    // ------------------------------------------------------------------

    private IEnumerator AddTextLine(string line, float fadeTime)
    {
        isTypingLine = true;
        isWaitingForInput = false;
        skipFade = false;

        accumulatedLines.Add(line);

        cutsceneText.text = BuildText(accumulatedLines.Count - 1, "00");

        yield return FadeInNewLine(accumulatedLines.Count - 1, fadeTime);

        cutsceneText.text = string.Join("\n", accumulatedLines);

        isTypingLine = false;
        skipFade = false;
        isWaitingForInput = true;
    }

    private string BuildText(int fadingIndex, string alphaHex)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < accumulatedLines.Count; i++)
        {
            sb.Append(i == fadingIndex ? $"<alpha=#{alphaHex}>" : "<alpha=#FF>");
            sb.Append(accumulatedLines[i]);

            if (i < accumulatedLines.Count - 1)
                sb.Append("\n");
        }

        return sb.ToString();
    }

    private IEnumerator FadeInNewLine(int lineIndex, float fadeTime)
    {
        float elapsed = 0;

        while (elapsed < fadeTime && !skipFade)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeTime);
            string alphaHex = ((byte)(alpha * 255)).ToString("X2");

            cutsceneText.text = BuildText(lineIndex, alphaHex);
            yield return null;
        }
    }

    private void ClearText()
    {
        accumulatedLines.Clear();
        cutsceneText.text = "";
    }

    // ------------------------------------------------------------------
    // Graphics (optional)
    // ------------------------------------------------------------------

    private void ShowGraphic(Sprite graphic, float fadeTime, bool clearTextBefore)
    {
        if (clearTextBefore)
            ClearText();

        if (graphicImage != null)
        {
            graphicImage.sprite = graphic;
            graphicImage.gameObject.SetActive(true);
            graphicImage.color = new Color(1, 1, 1, 0);

            if (fadeTime > 0)
            {
                currentCoroutine = StartCoroutine(FadeInGraphic(fadeTime));
            }
            else
            {
                graphicImage.color = Color.white;
                isWaitingForInput = true;
            }
        }
        else
        {
            AdvanceToNextStep();
        }
    }

    private IEnumerator FadeInGraphic(float fadeTime)
    {
        float elapsed = 0;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            graphicImage.color = new Color(1, 1, 1, Mathf.Lerp(0, 1, elapsed / fadeTime));
            yield return null;
        }
        graphicImage.color = Color.white;
        isWaitingForInput = true;
    }

    private void HideGraphic(float fadeTime)
    {
        if (graphicImage != null && graphicImage.gameObject.activeSelf)
        {
            if (fadeTime > 0)
            {
                currentCoroutine = StartCoroutine(FadeOutGraphic(fadeTime));
            }
            else
            {
                graphicImage.gameObject.SetActive(false);
                AdvanceToNextStep();
            }
        }
        else
        {
            AdvanceToNextStep();
        }
    }

    private IEnumerator FadeOutGraphic(float fadeTime)
    {
        float elapsed = 0;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            graphicImage.color = new Color(1, 1, 1, Mathf.Lerp(1, 0, elapsed / fadeTime));
            yield return null;
        }
        graphicImage.gameObject.SetActive(false);
        graphicImage.color = Color.white;
        AdvanceToNextStep();
    }

    // ------------------------------------------------------------------
    // Audio / delay
    // ------------------------------------------------------------------

    private void PlayAudio(AudioClip audio)
    {
        if (cutsceneAudio != null && audio != null)
        {
            cutsceneAudio.Stop();
            cutsceneAudio.clip = audio;
            cutsceneAudio.Play();
        }
    }

    private IEnumerator DelayThenAdvance(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        AdvanceToNextStep();
    }

    // ------------------------------------------------------------------
    // Teleport (finds the player by the "Player" tag)
    // ------------------------------------------------------------------

    private Transform FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        return p != null ? p.transform : null;
    }

    private void TeleportPlayer(Vector2 position, string sceneName = "")
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            StartCoroutine(TeleportWithSceneLoad(position, sceneName));
            return;
        }

        Transform player = FindPlayer();
        if (player != null)
        {
            player.position = position;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                mainCamera.transform.position = new Vector3(position.x, position.y, mainCamera.transform.position.z);
        }
        else
        {
            Debug.LogError("[CutsceneController] No object tagged 'Player' found for teleport!");
        }
    }

    private IEnumerator TeleportWithSceneLoad(Vector2 position, string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        while (!asyncLoad.isDone)
            yield return null;

        yield return null;

        Transform player = FindPlayer();
        if (player != null)
            player.position = position;
    }

    public void TeleportToAct2Location()
    {
        TeleportPlayer(act2TeleportPosition, act2TeleportSceneName);
    }

    public void TeleportToTarget()
    {
        if (teleportTargetObject != null)
            TeleportPlayer(teleportTargetObject.transform.position, "");
        else
            Debug.LogWarning("[CutsceneController] No teleport target assigned!");
    }

    public void TeleportToTag(string tag)
    {
        GameObject target = GameObject.FindGameObjectWithTag(tag);
        if (target != null)
            TeleportPlayer(target.transform.position, "");
        else
            Debug.LogWarning($"[CutsceneController] No object found with tag: {tag}");
    }

    // ------------------------------------------------------------------
    // End
    // ------------------------------------------------------------------

    private void EndCutscene()
    {
        cutscenePanel.SetActive(false);
        accumulatedLines.Clear();
        isWaitingForInput = false;
        isTypingLine = false;
        IsPlaying = false;

        UnfreezePlayer();

        Action callback = currentOnComplete;
        CutsceneData finished = currentCutscene;
        currentOnComplete = null;

        callback?.Invoke();
        finished?.onCutsceneEnd?.Invoke();
        onAnyCutsceneEnd?.Invoke();
    }
}