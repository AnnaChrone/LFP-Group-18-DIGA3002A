using UnityEngine;
using UnityEngine.InputSystem; 

public class OrderSlipBoard : MonoBehaviour
{
    [Header("UI Element")]
    public GameObject interactPromptText; // Drag your "Press E to Interact" popup here

    private bool playerInRange = false;

    private void Start()
    {
        if (interactPromptText != null) interactPromptText.SetActive(false);
    }

    private void Update()
    {
        // Only allow interactions during Nighttime (before service starts)
        if (playerInRange && DayNightCycleManager.Instance.currentState == GameState.Nighttime)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) 
            {
                // Turn off prompt text while looking at the selection UI menu
                if (interactPromptText != null) interactPromptText.SetActive(false);
                
                DayNightCycleManager.Instance.PromptStartService();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && DayNightCycleManager.Instance.currentState == GameState.Nighttime)
        {
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
