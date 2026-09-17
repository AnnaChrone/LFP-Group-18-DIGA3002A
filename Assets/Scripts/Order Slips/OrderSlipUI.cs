using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sushi.Data;
using Sushi.Inventory;

public class OrderSlipUI : MonoBehaviour
{
    [Header("Layout Assignments")]
    public TMP_Text menuTitleText;
    public Image menuIconDisplay;
    public TMP_Text goldPayoutText;

    private ItemData targetedFish;
    private Inventory liveInventoryReference;
    private OrderManager runtimeManager;

    public void InitializeTicket(ItemData fish, Inventory inventory, OrderManager manager)
    {
        targetedFish = fish;
        liveInventoryReference = inventory;
        runtimeManager = manager;

        // Apply Data Fields cleanly via your custom properties
        if (menuTitleText != null) menuTitleText.text = fish.Label + " Sushi";
        
        if (menuIconDisplay != null)
        {
            menuIconDisplay.sprite = fish.icon;
            menuIconDisplay.color = fish.tint; // Respects modular scriptable object color tints
        }

        if (goldPayoutText != null)
        {
            // Value multiplier calculation: Restaurant fulfillment gives a bonus over raw cargo dumping
            int totalValue = Mathf.RoundToInt(fish.baseValue * 1.6f);
            goldPayoutText.text = $"+{totalValue} Gold";
        }
    }

    // Map this to your Ticket UI asset button click setup
    public void ClickServeRecipeButton()
    {
        if (liveInventoryReference == null || targetedFish == null) return;

        // Uses your inventory's native query search function
        if (liveInventoryReference.CountOf(targetedFish) > 0)
        {
            // Uses your script's native mutation feature to safely prune exactly 1 matching item
            liveInventoryReference.RemoveFirst(targetedFish);

            // TODO: Connect this placeholder slot into your global wallet/economy code
            Debug.Log($"Served {targetedFish.Label}! Item deducted out of bag.");

            runtimeManager.DismissTicket(gameObject);
        }
        else
        {
            Debug.LogWarning("You ran out of this fish species mid-shift! Cannot fulfill.");
        }
    }
}
