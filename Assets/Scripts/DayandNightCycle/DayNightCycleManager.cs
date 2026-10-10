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

    // --- NEW: DAY SHIFT LOCKOUT FLAG ---
    [HideInInspector] 
    public bool hasServedToday = false; 

    [Header("Teleport Locations")]
    public Transform dockSpawnPoint;
    public Transform restaurantSpawnPoint;

    [Header("UI Panels")]
    public GameObject restaurantTransitionPanel;
    public GameObject startServicePanel;
    public GameObject endDayPromptPanel; 

    [Header("Visual Transition Settings")]
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

    public void PromptStartService()
    {
        // Safety guard: Reject immediately if player has already served dinner tonight
        if (hasServedToday)
        {
            Debug.Log("You have already run restaurant service for tonight!");
            return;
        }

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
            if (currentState == GameState.RestaurantService && OrderManager.Instance != null)
            {
                OrderManager.Instance.StopServiceOrders();
            }
            
            if (countdownDisplayObject != null) countdownDisplayObject.SetActive(false);

            // --- RESET FLAG: Player goes back to the dock, meaning a new day resets the lockout ---
            hasServedToday = false; 

            StartCoroutine(TeleportSequence(dockSpawnPoint.position, GameState.Daytime));
        }
        else
        {
            SetPlayerControls(true);
        }
    }

    private IEnumerator TeleportSequence(Vector3 targetPosition, GameState nextState)
    {
        SetPlayerControls(false); 

        while (screenFaderCanvasGroup.alpha < 1f)
        {
            screenFaderCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        screenFaderCanvasGroup.alpha = 1f;

        player.transform.position = targetPosition;
        currentState = nextState;

        yield return new WaitForSeconds(0.4f); 

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
        
        // --- LOCKOUT TRIGGERS: Service is locked in for the rest of this night sequence ---
        hasServedToday = true; 
        
        SetPlayerControls(true); 
        
        if (countdownDisplayObject != null) countdownDisplayObject.SetActive(true);
        if (OrderManager.Instance != null) OrderManager.Instance.StartServiceOrders();

        float timeRemaining = serviceDuration;
        while (timeRemaining > 0)
        {
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

        currentState = GameState.Nighttime;
        Debug.Log("Restaurant shift complete! Walking around freely active. Menu interactions disabled.");
    }

    private void TransitionToDaytimeDirect()
    {
        currentState = GameState.Daytime;
        hasServedToday = false; 
        player.transform.position = dockSpawnPoint.position;
        SetPlayerControls(true);
    }
}
