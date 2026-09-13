using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AreaTransitionTrigger : MonoBehaviour
{
    [Header("UI Requirements")]
    public GameObject confirmationPanel;
    public TextMeshProUGUI promptText;

    [Header("Trigger Constraints")]
    public TimeOfDay requiredTime;
    public GameLocation requiredLocation;
    [TextArea] public string customPromptMessage = "Do you want to proceed?";

    [Header("UI Buttons")]
    public Button yesButton;
    public Button noButton;

    private bool playerInTrigger = false;

    void Start()
    {
        // Ensure buttons have clean listeners
        yesButton.onClick.AddListener(OnPressYes);
        noButton.onClick.AddListener(OnPressNo);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Check if the game is currently in the right state for this door to work
            if (TimeCycleManager.Instance.currentTime == requiredTime && 
                TimeCycleManager.Instance.currentLocation == requiredLocation)
            {
                playerInTrigger = true;
                ShowPrompt();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            HidePrompt();
        }
    }

    void ShowPrompt()
    {
        promptText.text = customPromptMessage;
        confirmationPanel.SetActive(true);
        // Optional: Freeze player movement here if needed
    }

    void HidePrompt()
    {
        confirmationPanel.SetActive(false);
        // Optional: Unfreeze player movement here if needed
    }

    void OnPressYes()
    {
        if (!playerInTrigger) return;
        
        HidePrompt();
        TimeCycleManager.Instance.ProgressTimeAndLocation();
    }

    void OnPressNo()
    {
        HidePrompt();
    }
}
