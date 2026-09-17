using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Replace with 'using TMPro;' if using TextMeshPro
using TMPro;

public enum GameState { Daytime, Nighttime, RestaurantService }

public class DayNightCycleManager : MonoBehaviour
{
    public static DayNightCycleManager Instance;

    [Header("State Settings")]
    public GameState currentState = GameState.Daytime;
    public float serviceDuration = 60f; 

    [Header("Teleport Locations")]
    public Transform dockSpawnPoint;
    public Transform restaurantSpawnPoint;

    [Header("UI Panels")]
    public GameObject restaurantTransitionPanel;
    public GameObject startServicePanel;

    [Header("Visual Transition Settings")]
    public CanvasGroup screenFaderCanvasGroup;
    public float fadeSpeed = 2f;

    [Header("Visual Countdown Settings")]
    public TMP_Text countdownText;
    public GameObject countdownDisplayObject; // Parent object of the clock to hide/show it

    [Header("References")]
    public GameObject player;
    // Optional: Reference to your player movement script to disable it during fades
    // public PlayerMovement playerMovement; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Force clean initial state
        screenFaderCanvasGroup.alpha = 0f;
        if (countdownDisplayObject != null) countdownDisplayObject.SetActive(false);
        
        // Initialize at the dock
        TransitionToDaytimeDirect ();
    }

    // --- BUTTON TRIGGER FUNCTIONS ---

    public void PromptGoToRestaurant()
    {
        restaurantTransitionPanel.SetActive(true);
    }

    public void ConfirmGoToRestaurant(bool choice)
    {
        restaurantTransitionPanel.SetActive(false);
        if (choice)
        {
            StartCoroutine(TeleportSequence(restaurantSpawnPoint.position, GameState.Nighttime));
        }
    }

    public void PromptStartService()
    {
        startServicePanel.SetActive(true);
    }

    public void ConfirmStartService(bool choice)
    {
        startServicePanel.SetActive(false);
        if (choice)
        {
            StartCoroutine(RunRestaurantServiceSequence());
        }
    }

    // --- CO-ROUTINES FOR VISUALS & TRANSITIONS ---

    // Unified transition handler that fades out, teleports, and fades back in safely
    private IEnumerator TeleportSequence(Vector3 targetPosition, GameState nextState)
    {
        // 1. Freeze Player Control (Good practice so they don't move while screen is black)
        // if (playerMovement != null) playerMovement.enabled = false;

        // 2. Fade to Solid Black
        while (screenFaderCanvasGroup.alpha < 1f)
        {
            screenFaderCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        screenFaderCanvasGroup.alpha = 1f;

        // 3. Perform World Modifications Safely While Screen is Dark
        player.transform.position = targetPosition;
        currentState = nextState;

        // Small stall to give Cinemachine or camera scripts a frame to update positioning
        yield return new WaitForSeconds(0.2f); 

        // 4. Fade Back to Gameplay
        while (screenFaderCanvasGroup.alpha > 0f)
        {
            screenFaderCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        screenFaderCanvasGroup.alpha = 0f;

        // 5. Unfreeze Player Control
        // if (playerMovement != null) playerMovement.enabled = true;
    }

    // Handles the active shift countdown timer before cycling back home
    private IEnumerator RunRestaurantServiceSequence()
    {
        currentState = GameState.RestaurantService;
        
        // Show the clock UI
        if (countdownDisplayObject != null) countdownDisplayObject.SetActive(true);
        
        float timeRemaining = serviceDuration;

        // Visual Countdown Loop
        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            
            // Protect against falling below zero visually
            float displayTime = Mathf.Max(0, timeRemaining); 

            if (countdownText != null)
            {
                // Formats the raw float seconds into MM:SS digital format
                int minutes = Mathf.FloorToInt(displayTime / 60f);
                int seconds = Mathf.FloorToInt(displayTime % 60f);
                countdownText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }

            yield return null;
        }

        // Hide the clock UI now that dinner shift is over
        if (countdownDisplayObject != null) countdownDisplayObject.SetActive(false);

        // Shift complete: Fade out and dump player back at the starting dock for daytime
        yield return StartCoroutine(TeleportSequence(dockSpawnPoint.position, GameState.Daytime));
    }

    private void TransitionToDaytimeDirect()
    {
        currentState = GameState.Daytime;
        player.transform.position = dockSpawnPoint.position;
    }
}
