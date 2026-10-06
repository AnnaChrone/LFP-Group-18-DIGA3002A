using UnityEngine;
using UnityEngine.InputSystem;

public class BaitShopTrigger : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject shopUIPanel;
    [SerializeField] private GameObject interactPromptText;

    private bool playerInRange = false;

    void Update()
    {
        if (Keyboard.current == null) return;

        if (playerInRange && Keyboard.current.eKey.wasPressedThisFrame)
        {
            bool isShopActive = shopUIPanel.activeSelf;
            shopUIPanel.SetActive(!isShopActive);

            interactPromptText.SetActive(isShopActive);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;

            if (!shopUIPanel.activeSelf)
            {
                interactPromptText.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            shopUIPanel.SetActive(false);
            interactPromptText.SetActive(false);
        }
    }
}