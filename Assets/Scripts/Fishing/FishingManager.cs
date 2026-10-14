using Sushi.Data;
using Sushi.Fishing;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishingManager : MonoBehaviour
{
    public FishingState currentState = FishingState.NotFishing;

    [Header("Inventory")]
    public FishingCatchHandler catchHandler;

    [Header("Fishing Range")]
    public bool inFishingRange = false;
    public bool busyFishing = false;

    [Header("Casting")]
    public float minimumBiteTime = 2f;
    public float maximumBiteTime = 5f;

    private float biteTimer;

    [Header("Fish")]
    public float maximumFishDistance = 100f;
    public float fishDistance = 80f;
    public float fishPullSpeed = 3f;

    [Header("Rod")]
    [Tooltip("The single hook slot where bait is attached before casting.")]
    public BaitHook baitHook;
    public GameObject baitingPanel;
    public GameObject baitedRod;
    public GameObject ActiveFishingRod;

    [Header("Fish Resistance")]
    public float minimumResistance = 0.2f;
    public float maximumResistance = 1f;

    public float currentResistance;
    private float targetResistance;

    private float activeMinResistance;
    private float activeMaxResistance;

    public float minimumResistanceChangeTime = 0.5f;
    public float maximumResistanceChangeTime = 2f;

    private float resistanceChangeTimer;

    [Header("Rod Tension")]
    public float maximumRodTension = 50f;
    public float rodTension = 0f;
    public float reelSpeed = 10f;
    public float rodRecoverySpeed = 12f;
    public float resistanceTensionMultiplier = 25f;

    [Header("Fight Scoring")]
    [Range(0.5f, 0.99f)] public float highTensionThreshold = 0.9f;

    [Header("Tension Curve")]
    public float minimumTension = 5f;
    public float maximumTension = 45f;
    public float tensionExponent = 2.5f;

    [Header("Resistance")]
    public float resistanceChangeSpeed = 0.5f;

    [Header("Reeling")]
    public bool isReeling = false;

    [Header("Bobber")]
    public GameObject Bobber;
    public Transform BobberStart;
    public Transform BobberEnd;
    public float bobberDipAmount = 0.5f;

    [Header("Sound")]
    public float splashDelay = 0.4f;

    private float fightDuration;
    private float timeAtHighTension;
    private float resistanceTimeSum;

    [Header("Instruction UI")]
    public TextMeshProUGUI PressE;
    public TextMeshProUGUI PressSpace;
    public GameObject CaughtFishUI;
    public Image FishImage;

    [Tooltip("Optional. If the CaughtFishUI has a CanvasGroup, this lets us fade it smoothly.")]
    public CanvasGroup caughtFishCanvasGroup;

    [Header("Message Timing")]
    public float resultMessageDuration = 3f;

    [Tooltip("How long the caught fish icon stays fully visible before fading.")]
    public float caughtFishHoldTime = 0.5f;

    [Header("Tension Visuals")]
    [Range(0f, 1f)] public float yellowTensionThreshold = 0.60f;
    [Range(0f, 1f)] public float redTensionThreshold = 0.75f;

    [Header("Rod Colors")]
    public Color normalRodColor = Color.white;
    public Color yellowRodColor = Color.yellow;
    public Color redRodColor = Color.red;

    [Header("Line Colors")]
    public Color normalLineColor = Color.black;
    public Color yellowLineColor = Color.yellow;
    public Color redLineColor = Color.red;

    public bool smoothTensionColors = true;

    public SpriteRenderer activeRodRenderer;
    public LineRenderer fishingLine;

    public static FishingManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Bobber.SetActive(false);
        baitingPanel.SetActive(false);

        if (ActiveFishingRod != null)
        {
            activeRodRenderer = ActiveFishingRod.GetComponent<SpriteRenderer>();

            if (activeRodRenderer == null)
                activeRodRenderer = ActiveFishingRod.GetComponentInChildren<SpriteRenderer>();
        }

        // Auto-find the canvas group on the CaughtFishUI if one exists
        if (CaughtFishUI != null && caughtFishCanvasGroup == null)
            caughtFishCanvasGroup = CaughtFishUI.GetComponent<CanvasGroup>();

        // Hide the popup on startup
        if (CaughtFishUI != null)
            CaughtFishUI.SetActive(false);
    }

    private void Update()
    {
        if (currentState == FishingState.WaitingForFish)
        {
            WaitForFish();
        }

        if (currentState == FishingState.FishHooked)
        {
            UpdateFishing();
        }

        if (ActiveFishingRod != null)
        {
            ActiveFishingRod.transform.localRotation = Quaternion.Euler(0f, isReeling ? 0f : 180f, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Fishing"))
        {
            inFishingRange = true;
            PressE.text = "Press E to sit down";
            Debug.Log("In fishing range");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Fishing"))
        {
            inFishingRange = false;
            PressE.text = "";
            Debug.Log("Out of fishing range");
        }
    }

    public void InteractPressed()
    {
        if (currentState == FishingState.NotFishing)
        {
            SitDown();
            busyFishing = true;
        }
        else if (currentState == FishingState.Sitting)
        {
            StandUp();
            busyFishing = false;
        }
        else if (currentState == FishingState.WaitingForFish)
        {
            WithdrawLine();
        }
        else if (currentState == FishingState.FishHooked)
        {
            WithdrawLine();
        }
    }

    public void FishPressed()
    {
        if (currentState == FishingState.Sitting)
        {
            Cast();
        }
        else if (currentState == FishingState.FishHooked)
        {
            StartReeling();
        }
    }

    public void FishReleased()
    {
        if (currentState == FishingState.FishHooked)
        {
            StopReeling();
        }
    }

    private void SitDown()
    {
        baitingPanel.SetActive(true);
        baitedRod.SetActive(true);
        currentState = FishingState.Sitting;

        Debug.Log("Player sat down");
        PressSpace.text = "Press [SPACEBAR] to cast your line";
        PressE.text = "Press E to stop fishing";
    }

    private void StandUp()
    {
        currentState = FishingState.NotFishing;

        Debug.Log("Player stood up");

        if (baitHook != null) baitHook.ClearHook();
        baitingPanel.SetActive(false);
        baitedRod.SetActive(false);
        PressSpace.text = "";
        PressE.text = "Press E to sit down";
    }

    private void Cast()
    {
        if (baitHook == null || baitHook.attachedBait == null)
        {
            Debug.Log("Cannot cast: no bait on the hook!");
            PressSpace.text = "Cannot cast: no bait on the hook!";
            return;
        }

        if (catchHandler != null && !catchHandler.CanStartFishing(out string reason))
        {
            Debug.Log("Cannot cast: " + reason);
            return;
        }

        BaitData castBait = baitHook.ConsumeAndGetBaitForCast();
        if (castBait == null)
        {
            Debug.Log("Cannot cast: bait ran out!");
            return;
        }

        if (catchHandler != null)
        {
            catchHandler.SetBaitForThisCast(castBait);
        }

        currentState = FishingState.Casting;
        Bobber.transform.position = BobberStart.position;
        Bobber.SetActive(true);
        Debug.Log($"Cast! Using bait: {castBait.displayName}");
        baitingPanel.SetActive(false);
        baitedRod.SetActive(false);
        ActiveFishingRod.SetActive(true);
        PressSpace.text = "Wait for fish to bite...";

        SoundManager.Instance?.PlayCast();
        Invoke(nameof(PlaySplashSound), splashDelay);

        StartWaitingForFish();
    }

    private void PlaySplashSound()
    {
        SoundManager.Instance?.PlaySplash();
    }

    private void StartWaitingForFish()
    {
        currentState = FishingState.WaitingForFish;

        biteTimer = Random.Range(minimumBiteTime, maximumBiteTime);

        Debug.Log("Waiting for fish to bite...");
    }

    private void WaitForFish()
    {
        biteTimer -= Time.deltaTime;

        if (biteTimer <= 0f)
        {
            HookFish();
        }
    }

    private void HookFish()
    {
        currentState = FishingState.FishHooked;

        Debug.Log("FISH HOOKED!");
        PressSpace.text = "Hold [SPACEBAR] to reel fish!";
        Bobber.transform.Translate(0f, -bobberDipAmount, 0f, Space.World);

        fishDistance = maximumFishDistance;
        rodTension = 0f;
        isReeling = false;

        fightDuration = 0f;
        timeAtHighTension = 0f;
        resistanceTimeSum = 0f;

        ResetRodAndLineColor();

        activeMinResistance = minimumResistance;
        activeMaxResistance = maximumResistance;

        if (catchHandler != null)
        {
            catchHandler.RollHookedFish(out activeMinResistance, out activeMaxResistance);
        }

        currentResistance = Random.Range(activeMinResistance, activeMaxResistance);
        SetNewResistanceTarget();
        Debug.Log("Fish resistance: " + currentResistance);
    }

    private void StartReeling()
    {
        isReeling = true;
        SoundManager.Instance?.StartReel();
        Debug.Log("Started reeling");
    }

    private void StopReeling()
    {
        isReeling = false;
        SoundManager.Instance?.StopReel();
        Debug.Log("Stopped reeling");
    }

    private void UpdateFishing()
    {
        fightDuration += Time.deltaTime;
        resistanceTimeSum += currentResistance * Time.deltaTime;

        if (maximumRodTension > 0f && rodTension / maximumRodTension >= highTensionThreshold)
        {
            timeAtHighTension += Time.deltaTime;
        }

        UpdateResistance();

        if (isReeling)
        {
            ReelFish();
        }
        else
        {
            ReleaseLine();
        }

        CheckRodTension();
        CheckFishDistance();
        UpdateBobberPosition();
        UpdateRodTensionColor();
    }

    private void UpdateRodTensionColor()
    {
        if (maximumRodTension <= 0f) return;

        float t = Mathf.Clamp01(rodTension / maximumRodTension);

        Color rodColor = EvaluateTensionColor(t, normalRodColor, yellowRodColor, redRodColor);
        Color lineColor = EvaluateTensionColor(t, normalLineColor, yellowLineColor, redLineColor);

        if (activeRodRenderer != null)
            activeRodRenderer.color = rodColor;

        if (fishingLine != null)
        {
            fishingLine.startColor = lineColor;
            fishingLine.endColor = lineColor;
        }
    }

    private Color EvaluateTensionColor(float t, Color normal, Color yellow, Color red)
    {
        if (!smoothTensionColors)
        {
            if (t >= redTensionThreshold) return red;
            if (t >= yellowTensionThreshold) return yellow;
            return normal;
        }

        // Stay at normal until the yellow threshold
        if (t <= yellowTensionThreshold)
            return normal;

        // Blend normal -> yellow between yellow and red thresholds
        if (t <= redTensionThreshold)
        {
            float range = redTensionThreshold - yellowTensionThreshold;
            float a = range > 0f ? (t - yellowTensionThreshold) / range : 1f;
            return Color.Lerp(normal, yellow, a);
        }

        // Blend yellow -> red between red threshold and 1.0
        float redRange = 1f - redTensionThreshold;
        float aRed = redRange > 0f ? (t - redTensionThreshold) / redRange : 1f;
        return Color.Lerp(yellow, red, aRed);
    }

    private void ResetRodAndLineColor()
    {
        if (activeRodRenderer != null)
            activeRodRenderer.color = normalRodColor;

        if (fishingLine != null)
        {
            fishingLine.startColor = normalLineColor;
            fishingLine.endColor = normalLineColor;
        }
    }

    private void UpdateResistance()
    {
        resistanceChangeTimer -= Time.deltaTime;

        if (resistanceChangeTimer <= 0f)
        {
            SetNewResistanceTarget();
        }

        currentResistance = Mathf.MoveTowards(currentResistance, targetResistance, resistanceChangeSpeed * Time.deltaTime);
    }

    private void SetNewResistanceTarget()
    {
        targetResistance = Random.Range(activeMinResistance, activeMaxResistance);
        resistanceChangeTimer = Random.Range(minimumResistanceChangeTime, maximumResistanceChangeTime);
    }

    private void ReelFish()
    {
        float effectiveReelSpeed = reelSpeed * (1f - currentResistance);
        fishDistance -= effectiveReelSpeed * Time.deltaTime;

        float curvedResistance = Mathf.Pow(Mathf.Clamp01(currentResistance), tensionExponent);
        float tensionIncrease = Mathf.Lerp(minimumTension, maximumTension, curvedResistance);
        rodTension += tensionIncrease * Time.deltaTime;
    }

    private void ReleaseLine()
    {
        rodTension -= rodRecoverySpeed * Time.deltaTime;
        rodTension = Mathf.Max(rodTension, 0f);

        float effectivePullSpeed = fishPullSpeed * currentResistance;
        fishDistance += effectivePullSpeed * Time.deltaTime;
        fishDistance = Mathf.Min(fishDistance, maximumFishDistance);
    }

    private void UpdateBobberPosition()
    {
        float reelPercentage = 1f - (fishDistance / maximumFishDistance);

        float newX = Mathf.Lerp(BobberStart.position.x, BobberEnd.position.x, reelPercentage);

        Bobber.transform.position = new Vector3(
            newX,
            Bobber.transform.position.y,
            Bobber.transform.position.z
        );
    }

    private void CheckRodTension()
    {
        if (rodTension >= maximumRodTension)
        {
            BreakLine();
        }
    }

    private void BreakLine()
    {
        Debug.Log("Line broke :(");

        SoundManager.Instance.PlayBreak();
        PressSpace.text = "Fishing line broke! Tension reached maximum";

        isReeling = false;
        SoundManager.Instance?.StopReel();

        if (catchHandler != null) catchHandler.ClearHookedFish();

        rodTension = 0f;
        currentState = FishingState.Withdrawing;

        FinishWithdrawing();
        StartCoroutine(RestorePromptAfterDelay(resultMessageDuration));
    }

    private void CheckFishDistance()
    {
        if (fishDistance <= 0f)
        {
            CatchFish();
        }
    }

    private void CatchFish()
    {
        fishDistance = 0f;
        isReeling = false;
        SoundManager.Instance?.StopReel();
        SoundManager.Instance?.PlayCatch();

        if (catchHandler != null)
        {
            float averageResistance = fightDuration > 0f
                ? resistanceTimeSum / fightDuration
                : currentResistance;

            var stored = catchHandler.ResolveCatch(
                fightDuration,
                timeAtHighTension,
                averageResistance
            );

            if (stored.IsValid)
            {
                Debug.Log("Caught and stored: " + stored.Label);
                PressSpace.text = "Fish caught!";

                ShowCaughtFishUI(stored.data);
            }
        }
        else
        {
            Debug.Log("Fish was caught, but no catch handler is assigned.");
        }

        rodTension = 0f;
        currentState = FishingState.Withdrawing;

        FinishWithdrawing();
        StartCoroutine(RestorePromptAfterDelay(resultMessageDuration));
    }

    private void ShowCaughtFishUI(ItemData caught)
    {
        if (CaughtFishUI == null) return;

        // Set the icon
        if (FishImage != null && caught != null && caught.icon != null)
        {
            FishImage.sprite = caught.icon;
            FishImage.color = caught.tint;
            FishImage.enabled = true;
        }

        // Reset alpha and enable
        if (caughtFishCanvasGroup != null)
        {
            caughtFishCanvasGroup.alpha = 1f;
            caughtFishCanvasGroup.interactable = false;
            caughtFishCanvasGroup.blocksRaycasts = false;
        }

        CaughtFishUI.SetActive(true);

        // Fade out alongside the text
        StartCoroutine(FadeCaughtFishUI(resultMessageDuration));
    }

    private IEnumerator FadeCaughtFishUI(float duration)
    {
        if (CaughtFishUI == null) yield break;

        // If there's no canvas group, just wait then hide
        if (caughtFishCanvasGroup == null)
        {
            yield return new WaitForSeconds(duration);
            CaughtFishUI.SetActive(false);
            yield break;
        }

        // Hold at full opacity briefly, then fade
        float holdTime = Mathf.Clamp(caughtFishHoldTime, 0f, duration);
        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);

        float fadeTime = Mathf.Max(0.01f, duration - holdTime);
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            caughtFishCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }

        caughtFishCanvasGroup.alpha = 0f;
        CaughtFishUI.SetActive(false);
    }

    private IEnumerator RestorePromptAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (currentState == FishingState.Sitting)
        {
            PressSpace.text = "Press [SPACEBAR] to cast your line";
        }
    }

    private void WithdrawLine()
    {
        currentState = FishingState.Withdrawing;

        isReeling = false;
        SoundManager.Instance?.StopReel();

        if (catchHandler != null) catchHandler.ClearHookedFish();

        Debug.Log("Withdrawing fishing line");

        FinishWithdrawing();
    }

    private void FinishWithdrawing()
    {
        currentState = FishingState.Sitting;
        Bobber.SetActive(false);
        ActiveFishingRod.SetActive(false);
        baitedRod.SetActive(true);
        baitingPanel.SetActive(true);

        ResetRodAndLineColor();

        Debug.Log("Line withdrawn");
    }

    public void ForceStopFishing()
    {
        isReeling = false;
        SoundManager.Instance?.StopReel();
        rodTension = 0f;
        fishDistance = maximumFishDistance;

        if (catchHandler != null) catchHandler.ClearHookedFish();
        if (Bobber != null) Bobber.SetActive(false);

        if (baitHook != null) baitHook.ClearHook();

        // Also hide the caught-fish popup if it's showing
        if (CaughtFishUI != null)
            CaughtFishUI.SetActive(false);

        ResetRodAndLineColor();

        currentState = FishingState.NotFishing;
        busyFishing = false;
    }
}