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

        // Initialise fish resistance here later.
    }


    private void StartReeling()
    {
        isReeling = true;

        Debug.Log("Started reeling!");
    }


    private void StopReeling()
    {
        isReeling = false;

        Debug.Log("Stopped reeling!");
    }


    private void UpdateFishing()
    {
        if (isReeling)
        {
            ReelFish();
        }
        else
        {
            ReleaseLine();
        }
    }


    private void ReelFish()
    {
        // Fish/rod mechanics will go here.
    }


    private void ReleaseLine()
    {
        // Rod recovery will go here.
    }


    private void WithdrawLine()
    {
        currentState = FishingState.Withdrawing;

        isReeling = false;

        Debug.Log("Withdrawing fishing line");

        // Animation/line withdrawal will go here.

        FinishWithdrawing();
    }


    private void FinishWithdrawing()
    {
        currentState = FishingState.Sitting;

        Debug.Log("Line withdrawn");
    }
}