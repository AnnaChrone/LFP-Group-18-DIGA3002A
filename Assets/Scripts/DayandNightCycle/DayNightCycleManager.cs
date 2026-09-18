using UnityEngine;
using System.Collections;
using UnityEngine.UI; 
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
    private MonoBehaviour playerMovementComponent; // Acts as a handle to freeze movement

    
   private void Start()
    {
        // 1. Automatically find the movement script attached to the player object
        if (player != null)
        {
            playerMovementComponent = player.GetComponent<MonoBehaviour>(); 
        }

        // 2. Clear visual overlays on startup
        screenFaderCanvasGroup.alpha = 0f;
        if (countdownDisplayObject != null) countdownDisplayObject.SetActive(false);
        
        TransitionToDaytimeDirect();
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- HELPER FUNCTION TO FREEZE/UNFREEZE PLAYER ---
    public void SetPlayerControls(bool state)
    {
        if (playerMovementComponent != null)
        {
            playerMovementComponent.enabled = state;
        }

        // Stops sliding momentum when frozen if the player uses a Rigidbody2D
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null && !state)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero; 
#endif
        }
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
        while (screenFaderCanvasGroup.alpha < 1f)
        {
            screenFaderCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        screenFaderCanvasGroup.alpha = 1f;

        // Perform World Modifications Safely While Screen is Dark
        player.transform.position = targetPosition;
        currentState = nextState;

        // Small stall to give Cinemachine or camera scripts a frame to update positioning
        yield return new WaitForSeconds(0.2f); 

        // Fade Back to Gameplay
        while (screenFaderCanvasGroup.alpha > 0f)
        {
            screenFaderCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        screenFaderCanvasGroup.alpha = 0f;

    }

    // Handles the active shift countdown timer before cycling back home
      private IEnumerator RunRestaurantServiceSequence()
    {
        currentState = GameState.RestaurantService;
        SetPlayerControls(true);
        
        if (countdownDisplayObject != null) countdownDisplayObject.SetActive(true);
        
        // --- HOOK: ACTIVATE THE TIMED GENERATION DISPATCHER ---
        if (OrderManager.Instance != null) OrderManager.Instance.StartServiceOrders();

        float timeRemaining = serviceDuration;
        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            float displayTime = Mathf.Max(0, timeRemaining); 

            if (countdownText != null)
            {
                int minutes = Mathf.FloorToInt(displayTime / 60f);
                int seconds = Mathf.FloorToInt(displayTime % 60f);
                countdownText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
            yield return null;
        }

        if (countdownDisplayObject != null) countdownDisplayObject.SetActive(false);

        // --- HOOK: TERMINATE THE REQUISITIONS LOOP & FLUSH REMAINING SLIPS ---
        if (OrderManager.Instance != null) OrderManager.Instance.StopServiceOrders();

        yield return StartCoroutine(TeleportSequence(dockSpawnPoint.position, GameState.Daytime));
    }

    private void TransitionToDaytimeDirect()
    {
        currentState = GameState.Daytime;
        player.transform.position = dockSpawnPoint.position;
    }


}
