using UnityEngine;

public class OrderSlipBoard : MonoBehaviour
{
    private bool playerInRange = false;

    private void Update()
    {
        // Only allow interaction if it is nighttime and player presses 'E'
        if (playerInRange && DayNightCycleManager.Instance.currentState == GameState.Nighttime)
        {
            if (Input.GetKeyDown(KeyCode.E)) 
            {
                DayNightCycleManager.Instance.PromptStartService();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerDown2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) playerInRange = false;
    }
}
