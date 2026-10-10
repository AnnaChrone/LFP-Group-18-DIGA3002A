using UnityEngine;
using UnityEngine.InputSystem;

public enum InteractionTargetType { DockToRestaurantDoor, MenuStartShift, RestaurantToDockDoor }

public class InteractiveTrigger : MonoBehaviour
{
    [Header("Configuration")]
    public InteractionTargetType triggerType;
    public GameObject interactPromptText; // "Press E to Interact" canvas floating label element

    private bool playerInRange = false;

    private void Start()
    {
        if (interactPromptText != null) interactPromptText.SetActive(false);
    }

    private void Update()
    {
        if (playerInRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TriggerTargetAction();
        }
    }

    private void TriggerTargetAction()
    {
        // Hide interaction visual tracking labels while menu windows are drawn active
        if (interactPromptText != null) interactPromptText.SetActive(false);

        switch (triggerType)
        {
            case InteractionTargetType.DockToRestaurantDoor:
                // Only allow exiting the dock during daytime hours
                if (DayNightCycleManager.Instance.currentState == GameState.Daytime)
                {
                    DayNightCycleManager.Instance.PromptGoToRestaurant();
                }
                break;

            case InteractionTargetType.MenuStartShift:
                // Only allow running restaurant service if it's currently a quiet nighttime block
                if (DayNightCycleManager.Instance.currentState == GameState.Nighttime)
                {
                    DayNightCycleManager.Instance.PromptStartService();
                }
                break;

            case InteractionTargetType.RestaurantToDockDoor:
                // Allowed at any time, even mid-shift service durations
                DayNightCycleManager.Instance.PromptEndDay();
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Extra constraint validation check logic to avoid prompt pollution
            if (triggerType == InteractionTargetType.MenuStartShift && DayNightCycleManager.Instance.currentState != GameState.Nighttime) return;
            if (triggerType == InteractionTargetType.DockToRestaurantDoor && DayNightCycleManager.Instance.currentState != GameState.Daytime) return;

            playerInRange = true;
            if (interactPromptText != null) interactPromptText.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactPromptText != null) interactPromptText.SetActive(false);
        }
    }
}
