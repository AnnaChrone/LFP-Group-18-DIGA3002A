using UnityEngine;

public class BaitShopTrigger : MonoBehaviour
{
    [SerializeField] private GameObject shopUIPanel;
    private bool playerInRange = false;

    void Update()
    {
        // Toggle shop UI when pressing E if the player is nearby
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            bool isShopActive = shopUIPanel.activeSelf;
            shopUIPanel.SetActive(!isShopActive);
            
            // Optional: Pause game or unlock cursor when shop opens
            Cursor.lockState = !isShopActive ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            // Optional: Show an "E to Interact" floating prompt here
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            shopUIPanel.SetActive(false); // Auto-close if player walks away
        }
    }
}
