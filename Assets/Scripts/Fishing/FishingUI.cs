
using UnityEngine;
using UnityEngine.UI;

public class FishingUI : MonoBehaviour
{
    [Header("Fishing Manager")]
    public FishingManager fishingManager;

    [Header("Fishing UI")]
    public GameObject fishingBars;

    [Header("UI Bars")]
    public Slider fishDistanceBar;
    public Slider rodTensionBar;
    public Slider resistanceBar;


    private void Start()
    {
        // Set all bars to use a 0-1 range.
        fishDistanceBar.minValue = 0f;
        fishDistanceBar.maxValue = 1f;

        rodTensionBar.minValue = 0f;
        rodTensionBar.maxValue = 1f;

        resistanceBar.minValue = 0f;
        resistanceBar.maxValue = 1f;

        // Hide the fishing UI when the game starts.
        fishingBars.SetActive(false);
    }


    private void Update()
    {
        // Only show the fishing UI while the player
        // is fighting a hooked fish.
        if (fishingManager.currentState == FishingState.FishHooked)
        {
            ShowFishingUI();
            UpdateBars();
        }
        else
        {
            HideFishingUI();
        }
    }


    private void UpdateBars()
    {
        /*
         * 1 = fish is far away
         * 0 = fish has reached the player
         */

        float fishDistancePercentage =
            fishingManager.fishDistance /
            fishingManager.maximumFishDistance;

        fishDistanceBar.value = fishDistancePercentage;


        /*
         * 0 = no tension
         * 1 = maximum tension / line breaks
         */

        float rodTensionPercentage =
            fishingManager.rodTension /
            fishingManager.maximumRodTension;

        rodTensionBar.value = rodTensionPercentage;


        /*
         * Convert resistance into a 0-1 value.
         */

        float resistancePercentage =
            Mathf.InverseLerp(
                fishingManager.minimumResistance,
                fishingManager.maximumResistance,
                fishingManager.currentResistance
            );

        resistanceBar.value = resistancePercentage;
    }


    private void ShowFishingUI()
    {
        if (!fishingBars.activeSelf)
        {
            fishingBars.SetActive(true);
        }
    }


    private void HideFishingUI()
    {
        if (fishingBars.activeSelf)
        {
            fishingBars.SetActive(false);
        }
    }
}
