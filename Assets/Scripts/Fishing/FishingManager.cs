using UnityEngine;

public class FishingManager : MonoBehaviour
{
    public FishingState currentState = FishingState.Idle;

    [Header("Casting")]
    public float minimumBiteTime = 2f;
    public float maximumBiteTime = 5f;

    private float biteTimer;

    private void Update()
    {
        if (currentState == FishingState.WaitingForFish)
        {
            WaitForFish();
        }
    }

    public void InteractPressed()
    {
        if (currentState == FishingState.Idle)
        {
            Cast();
        }
        else if (currentState == FishingState.FishHooked)
        {
            StartReeling();
        }
    }

    public void InteractReleased()
    {
        if (currentState == FishingState.FishHooked)
        {
            StopReeling();
        }
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

        biteTimer = Random.Range(minimumBiteTime,maximumBiteTime);

        Debug.Log("Waiting for fish to bite");
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
        //Need logic here that if the player doesnt start reeling in 2? seconds, they lose the fish and the process restarts
    }

    private void StartReeling()
    {
        Debug.Log("Started reeling!");
    }

    private void StopReeling()
    {
        Debug.Log("Stopped reeling!");
    }
}