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
    public GameObject endDayPromptPanel; // New Panel: "Are you ready to end the day?"

    [Header("Visual Transition Settings")]
    [Tooltip("This canvas group should now be on your Sunset Fader image.")]
    public CanvasGroup screenFaderCanvasGroup;
    public float fadeSpeed = 2f;

    [Header("Visual Countdown Settings")]
    public TMP_Text countdownText; 
    public GameObject countdownDisplayObject; 

    [Header("References")]
    public GameObject player;
    
    private MonoBehaviour playerMovementComponent; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (player != null)
        {
            playerMovementComponent = player.GetComponent<MonoBehaviour>(); 
        }

        screenFaderCanvasGroup.alpha = 0f;
        if (countdownDisplayObject != null) countdownDisplayObject.SetActive(false);
        if (endDayPromptPanel != null) endDayPromptPanel.SetActive(false);
        
        TransitionToDaytimeDirect();
    }

    public void SetPlayerControls(bool state)
    {
        if (playerMovementComponent != null) playerMovementComponent.enabled = state;

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

    // --- DOOR TO RESTAURANT PROMPTS ---
    public void PromptGoToRestaurant()
    {
        SetPlayerControls(false); 
        restaurantTransitionPanel.SetActive(true);
    }

    public void ConfirmGoToRestaurant(bool choice)
    {
        restaurantTransitionPanel.SetActive(false);
        if (choice)
        {
            StartCoroutine(TeleportSequence(restaurantSpawnPoint.position, GameState.Nighttime));
        }
        else
        {
            SetPlayerControls(true); 
        }
    }

    // --- MENU INTERACTION SHIFT PROMPTS ---
    public void PromptStartService()
    {
        SetPlayerControls(false); 
        startServicePanel.SetActive(true);
    }

    public void ConfirmStartService(bool choice)
    {
        startServicePanel.SetActive(false);
        if (choice)
        {
            StartCoroutine(RunRestaurantServiceSequence());
        }
        else
        {
            SetPlayerControls(true); 
        }
    }

    // --- END DAY RESTAURANT DOOR PROMPTS ---
    public void PromptEndDay()
    {
        SetPlayerControls(false);
        endDayPromptPanel.SetActive(true);
    }

    public void ConfirmEndDay(bool choice)
    {
        endDayPromptPanel.SetActive(false);
        if (choice)
        {
            // If they are busy with service, force clean up remaining tickets out of slots
            if (currentState == GameState.RestaurantService && OrderManager.Instance != null)
            {
                OrderManager.Instance.StopServiceOrders();
            }
            
            if (countdownDisplayObject != null) countdownDisplayObject.SetActive(false);

            StartCoroutine(TeleportSequence(dockSpawnPoint.position, GameState.Daytime));
        }
        else
        {
            SetPlayerControls(true);
        }
    }

    // --- TIMED TRANSITIONS SEQUENCES ---
    private IEnumerator TeleportSequence(Vector3 targetPosition, GameState nextState)
    {
        SetPlayerControls(false); 

        // Sunset visual fade overlay calculation loop
        while (screenFaderCanvasGroup.alpha < 1f)
        {
            screenFaderCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        screenFaderCanvasGroup.alpha = 1f;

        player.transform.position = targetPosition;
        currentState = nextState;

        yield return new WaitForSeconds(0.4f); // Slightly elongated to display sunset graphic clearly

        while (screenFaderCanvasGroup.alpha > 0f)
        {
            screenFaderCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        screenFaderCanvasGroup.alpha = 0f;

        SetPlayerControls(true); 
    }

    private IEnumerator RunRestaurantServiceSequence()
    {
        currentState = GameState.RestaurantService;
        SetPlayerControls(true); 
        
        if (countdownDisplayObject != null) countdownDisplayObject.SetActive(true);
        if (OrderManager.Instance != null) OrderManager.Instance.StartServiceOrders();

        float timeRemaining = serviceDuration;
        while (timeRemaining > 0)
        {
            // If player exits mid-shift using the door, terminate this coroutine routine execution early
            if (currentState == GameState.Daytime) yield break;

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
        if (OrderManager.Instance != null) OrderManager.Instance.StopServiceOrders();

        // INJECTION POINT OVERRIDE: Instead of automatic dock teleporting, flip state back to plain idle Nighttime
        currentState = GameState.Nighttime;
        Debug.Log("Restaurant shift complete! Walking around freely active.");
    }

    private void TransitionToDaytimeDirect()
    {
        currentState = GameState.Daytime;
        player.transform.position = dockSpawnPoint.position;
        SetPlayerControls(true);
    }
}
