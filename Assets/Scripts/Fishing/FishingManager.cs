using UnityEngine;

public class FishingManager : MonoBehaviour
{
    public FishingState currentState = FishingState.NotFishing;

    [Header("Fishing Range")]
    public bool inFishingRange = false;

    [Header("Casting")]
    public float minimumBiteTime = 2f;
    public float maximumBiteTime = 5f;

    private float biteTimer;

    [Header("Fish")] 
    public float maximumFishDistance = 100f;  //as far as the fish can be before the line breaks
    public float fishDistance = 80f; 
    public float fishPullSpeed = 5f; 

    [Header("Fish Resistance")] 
    public float minimumResistance = 0.2f; 
    public float maximumResistance = 1f; 

    public float currentResistance; 
    private float targetResistance; 

    public float minimumResistanceChangeTime = 0.5f; 
    public float maximumResistanceChangeTime = 2f; 

    private float resistanceChangeTimer;

    [Header("Rod")] 
    public float maximumRodTension = 50f; 
    public float rodTension = 0f; 
    public float reelSpeed = 10f; 
    public float rodRecoverySpeed = 12f;
    public float resistanceTensionMultiplier = 25f;

    [Header("Tension Curve")]
    public float minimumTension = 5f;    // tension/sec at resistance = 0
    public float maximumTension = 45f;   // tension/sec at resistance = 1
    public float tensionExponent = 2.5f; // higher = more punishing at high resistance

    [Header("Resistance")]
    public float resistanceChangeSpeed = 0.5f; // units per second

    [Header("Reeling")]
    public bool isReeling = false;


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
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Fishing"))
        {
            inFishingRange = true;

            Debug.Log("In fishing range");
        }
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Fishing"))
        {
            inFishingRange = false;

            Debug.Log("Out of fishing range");
        }
    }


    public void InteractPressed() //seperated logic for fishing and sitting/standing from fishing
    {
        if (currentState == FishingState.NotFishing)
        {
            SitDown();
        }
        else if (currentState == FishingState.Sitting)
        {
            StandUp();
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
        currentState = FishingState.Sitting;

        Debug.Log("Player sat down");

        // Disable player movement here later.
    }


    private void StandUp()
    {
        currentState = FishingState.NotFishing;

        Debug.Log("Player stood up");

        // Enable player movement here later.
    }


    private void Cast()
    {
        currentState = FishingState.Casting;

        Debug.Log("Cast!");

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

        fishDistance = maximumFishDistance; //gives the distance the fish is from being reeled in
        rodTension = 0f; 
        isReeling = false;

        currentResistance = Random.Range(minimumResistance,maximumResistance);
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
        targetResistance = Random.Range(minimumResistance, maximumResistance);

        resistanceChangeTimer = Random.Range(minimumResistanceChangeTime, maximumResistanceChangeTime); //decides how long the resistance change should take

    }

    private void ReelFish()
    {
        /*
         * The higher the resistance, the less effective the reeling is.
         * Resistance of 0.2 = easier to reel.
         * Resistance of 1.0 = difficult to reel.
         */

        float effectiveReelSpeed = reelSpeed * (1f - currentResistance);

        fishDistance -= effectiveReelSpeed * Time.deltaTime;


        // Normalizes resistance to 0-1
        float normalizedResistance = Mathf.InverseLerp(minimumResistance, maximumResistance, currentResistance);

        // Power curve allows low resistance stays low, high resistance spikes
        float curvedResistance = Mathf.Pow(normalizedResistance, tensionExponent);

        float tensionIncrease = Mathf.Lerp(minimumTension, maximumTension, curvedResistance);

        rodTension += tensionIncrease * Time.deltaTime;


        Debug.Log(
            "Reeling | Distance: " + fishDistance +
            " | Resistance: " + currentResistance +
            " | Tension: " + rodTension
        );
    }


    private void ReleaseLine()
    {
        rodTension -= rodRecoverySpeed * Time.deltaTime;

        rodTension = Mathf.Max(rodTension, 0f);

        //Higher resistance = stronger pull away from fish

        float effectivePullSpeed = fishPullSpeed * currentResistance;

        fishDistance += effectivePullSpeed * Time.deltaTime;

        fishDistance = Mathf.Min(fishDistance, maximumFishDistance);

        Debug.Log("Not reeling | Distance: " + fishDistance + " | Tension: " + rodTension);
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
        rodTension = 0f;
        Debug.Log("Fish was caught!!!!!!!!!!");
        //ADD CAUGHT FISH TO INV HERE

        currentState = FishingState.Withdrawing;
        FinishWithdrawing();

    }
    private void WithdrawLine()
    {
        currentState = FishingState.Withdrawing;

        isReeling = false;

        Debug.Log("Withdrawing fishing line");

        // Animation/line withdrawal will go here

        FinishWithdrawing();
    }


    private void FinishWithdrawing()
    {
        currentState = FishingState.Sitting;

        Debug.Log("Line withdrawn");
    }
}