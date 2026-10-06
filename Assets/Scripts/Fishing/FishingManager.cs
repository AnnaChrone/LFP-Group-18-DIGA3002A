using Sushi.Data;
using Sushi.Fishing;
using TMPro;
using UnityEngine;

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
    public float maximumFishDistance = 100f;  //as far as the fish can be before the line breaks
    public float fishDistance = 80f;
    public float fishPullSpeed = 3f;

    [Header("Rod")]
    [Tooltip("The single hook slot where bait is attached before casting.")]
    public BaitHook baitHook;
    public GameObject baitingPanel;
    public GameObject baitedRod;
    public GameObject ActiveFishingRod;

    [Header("Fish Resistance")]
    [Tooltip("Fallback range, used only when no catch handler is assigned. " +
            "With a handler, each species supplies its own range on bite.")]
    public float minimumResistance = 0.2f;
    public float maximumResistance = 1f;

    public float currentResistance;
    private float targetResistance;

    // The range for the fish currently on the line.
    private float activeMinResistance;
    private float activeMaxResistance;

    public float minimumResistanceChangeTime = 0.5f;
    public float maximumResistanceChangeTime = 2f;

    private float resistanceChangeTimer;

    [Header("Rod")]
    public float maximumRodTension = 50f;
    public float rodTension = 0f;
    public float reelSpeed = 10f;
    public float rodRecoverySpeed = 12f;
    public float resistanceTensionMultiplier = 25f;

    [Header("Fight Scoring")]
    [Tooltip("Rod tension above this fraction of maximum counts as 'in the danger zone'.")]
    [Range(0.5f, 0.99f)] public float highTensionThreshold = 0.9f;

    [Header("Tension Curve")]
    public float minimumTension = 5f;    // tension/sec at resistance = 0
    public float maximumTension = 45f;   // tension/sec at resistance = 1
    public float tensionExponent = 2.5f; // higher = more punishing at high resistance

    [Header("Resistance")]
    public float resistanceChangeSpeed = 0.5f; // units per second

    [Header("Reeling")]
    public bool isReeling = false;

    [Header("Bobber")]
    public GameObject Bobber;
    public Transform BobberStart;
    public Transform BobberEnd;
    public float bobberDipAmount = 0.5f;

    // Fight telemetry, read by the fish quality system to decide how heavy the catch is.
    private float fightDuration;
    private float timeAtHighTension; // seconds spent at or above the threshold
    private float resistanceTimeSum; // resistance * time, for the fight's average


    [Header("Instruction UI")]
    public TextMeshProUGUI PressE;
    public TextMeshProUGUI PressSpace;

    private void Start()
    {
        Bobber.SetActive(false);
        baitingPanel.SetActive(false);
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

        ActiveFishingRod.transform.localRotation = Quaternion.Euler(0f, isReeling ? 0f : 180f, 0f);
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


    public void InteractPressed() //seperated logic for fishing and sitting/standing from fishing
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

        // Clear the hook
        if (baitHook != null) baitHook.ClearHook();
        baitingPanel.SetActive(false);
        baitedRod.SetActive(false);
        PressSpace.text = "";
        PressE.text = "Press E to sit down";
    }


    private void Cast()
    {
        // Must have bait on the hook
        if (baitHook == null || baitHook.attachedBait == null)
        {
            Debug.Log("Cannot cast: no bait on the hook!");
            // Optional: UiPrompter or a message
            return;
        }

        // 2. Must have capacity in inventory
        if (catchHandler != null && !catchHandler.CanStartFishing(out string reason))
        {
            Debug.Log("Cannot cast: " + reason);
            return;
        }

        // 3. Consume 1 bait from inventory and get the type being cast with
        BaitData castBait = baitHook.ConsumeAndGetBaitForCast();
        if (castBait == null)
        {
            Debug.Log("Cannot cast: bait ran out!");
            return;
        }

        // 4. Tell the catch handler which bait to roll with
        if (catchHandler != null)
        {
            catchHandler.SetBaitForThisCast(castBait);
        }

        // 5. Existing cast flow
        currentState = FishingState.Casting;
        Bobber.transform.position = BobberStart.position;
        Bobber.SetActive(true);
        Debug.Log($"Cast! Using bait: {castBait.displayName}");
        baitingPanel.SetActive(false);
        baitedRod.SetActive(false);
        ActiveFishingRod.SetActive(true);
        //new rod here
        PressSpace.text = "Wait for fish to bite...";
        StartWaitingForFish();

    }


    private void StartWaitingForFish()
    {
        currentState = FishingState.WaitingForFish;

        biteTimer = Random.Range(
            minimumBiteTime,
            maximumBiteTime
        );

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
        PressSpace.text = "Hold [SPACEBAR] to reel fish! Make sure rod tension doesnt get too high!";
        Bobber.transform.Translate(0f,-bobberDipAmount,0f,Space.World);

        fishDistance = maximumFishDistance; //gives the distance the fish is from being reeled in
        rodTension = 0f;
        isReeling = false;

        // Reset fight telemetry for the new fish.
        fightDuration = 0f;
        timeAtHighTension = 0f;
        resistanceTimeSum = 0f;

        // The species is decided here, on the bite, and it sets how hard the fight is.
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

        Debug.Log("Started reeling");
    }


    private void StopReeling()
    {
        isReeling = false;

        Debug.Log("Stopped reeling");
    }


    private void UpdateFishing()
    {
        // Track how long and how roughly this fish was fought.
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

    }

    private void UpdateResistance()
    {
        resistanceChangeTimer -= Time.deltaTime; //sets a timer for when to change resistance

        if (resistanceChangeTimer <= 0f)
        {
            SetNewResistanceTarget();
        }

        currentResistance = Mathf.MoveTowards(currentResistance, targetResistance, resistanceChangeSpeed * Time.deltaTime);
    }

    private void SetNewResistanceTarget()
    {
        targetResistance = Random.Range(activeMinResistance, activeMaxResistance);

        resistanceChangeTimer = Random.Range(minimumResistanceChangeTime, maximumResistanceChangeTime); //decides how long the resistance change should take

    }

    private void ReelFish()
    {
        /*
         * The higher the resistance, the less effective the reeling is.
         * Resistance of 0 = effortless (seaweed).
         * Resistance of 1.0 = difficult to reel.
         */

        float effectiveReelSpeed = reelSpeed * (1f - currentResistance);

        fishDistance -= effectiveReelSpeed * Time.deltaTime;


        // Power curve on absolute resistance (0-1): low resistance stays low,
        // high resistance spikes. Absolute rather than range-normalised so a
        // species with a narrow range does not swing through the whole curve.
        float curvedResistance = Mathf.Pow(Mathf.Clamp01(currentResistance), tensionExponent);

        float tensionIncrease = Mathf.Lerp(minimumTension, maximumTension, curvedResistance);

        rodTension += tensionIncrease * Time.deltaTime;

    }


    private void ReleaseLine()
    {
        rodTension -= rodRecoverySpeed * Time.deltaTime;

        rodTension = Mathf.Max(rodTension, 0f);

        //Higher resistance = stronger pull away from fish

        float effectivePullSpeed = fishPullSpeed * currentResistance;

        fishDistance += effectivePullSpeed * Time.deltaTime;

        fishDistance = Mathf.Min(fishDistance, maximumFishDistance);

       // Debug.Log("Not reeling | Distance: " + fishDistance + " | Tension: " + rodTension);
    }

    private void UpdateBobberPosition()
    {
        // 0 = fish is at maximum distance
        // 1 = fish is fully reeled in
        float reelPercentage =
            1f - (fishDistance / maximumFishDistance);

        float newX = Mathf.Lerp(
            BobberStart.position.x,
            BobberEnd.position.x,
            reelPercentage
        );

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

        isReeling = false;
        //play anim for line break here

        // The fish got away.
        if (catchHandler != null) catchHandler.ClearHookedFish();

        rodTension = 0f;
        currentState = FishingState.Withdrawing;

        FinishWithdrawing();
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

        // The species was already decided on the bite. The handler scores the
        // fight and stores the fish, so this file needs no changes for new
        // fish or bait.
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
            }
        }
        else
        {
            Debug.Log("Fish was caught, but no catch handler is assigned.");
        }

        rodTension = 0f;
        currentState = FishingState.Withdrawing;
        FinishWithdrawing();

    }

    private void WithdrawLine()
    {
        currentState = FishingState.Withdrawing;

        isReeling = false;

        // Reeling the line in early lets the hooked fish go.
        if (catchHandler != null) catchHandler.ClearHookedFish();

        Debug.Log("Withdrawing fishing line");

        // Animation/line withdrawal will go here

        FinishWithdrawing();
    }


    private void FinishWithdrawing()
    {
        currentState = FishingState.Sitting;
        Bobber.SetActive(false);
        ActiveFishingRod.SetActive(false);
        baitedRod.SetActive(true);
        baitingPanel.SetActive(true);
        Debug.Log("Line withdrawn");
        PressSpace.text = "Press [SPACEBAR] to cast your line";
    }


    /// <summary>
    /// Hard stop, called by DayNightController when the night phase begins.
    /// Gets the player out of the chair no matter which state they were in,
    /// so a hooked fish cannot survive the phase change.
    /// </summary>
    public void ForceStopFishing()
    {
        isReeling = false;
        rodTension = 0f;
        fishDistance = maximumFishDistance;

        if (catchHandler != null) catchHandler.ClearHookedFish();
        if (Bobber != null) Bobber.SetActive(false);

        // Clear the hook so the player isn't left with stale bait after the phase change
        if (baitHook != null) baitHook.ClearHook();

        currentState = FishingState.NotFishing;
        busyFishing = false;
    }
}
