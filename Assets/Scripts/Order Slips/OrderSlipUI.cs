using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sushi.Data;
using Sushi.Inventory;

namespace Sushi.UI
{
    /// <summary>
    /// Attached to the individual Order Slip UI prefab instances.
    /// Extracts data from the chosen ItemData asset and updates the visual card fields.
    /// Handles the click interaction to serve the dish and deduct fish from the inventory.
    /// </summary>
    [DisallowMultipleComponent]
    public class OrderSlipUI : MonoBehaviour
    {
        [Header("UI Visual Assignments")]
        [Tooltip("The text mesh element that will display the dish name (e.g., 'Tuna Sushi').")]
        [SerializeField] private TMP_Text menuTitleText;

        [Tooltip("The image slot displaying the fish silhouette or sprite design.")]
        [SerializeField] private Image menuIconDisplay;

        [Tooltip("The text element showing how much gold this recipe rewards.")]
        [SerializeField] private TMP_Text goldPayoutText;

        private ItemData targetedFish;
        private Inventory.Inventory liveInventoryReference;
        private OrderManager runtimeManager;

        /// <summary>
        /// Instantiated tickets call this entry method to populate names, icons, and economy weights.
        /// </summary>
        public void InitializeTicket(ItemData fish, Inventory.Inventory inventory, OrderManager manager)
        {
            targetedFish = fish;
            liveInventoryReference = inventory;
            runtimeManager = manager;

            // Apply Display Text safely via your ScriptableObject's 'Label' property
            if (menuTitleText != null) 
            {
                menuTitleText.text = $"{fish.Label} Sushi";
            }
            
            // Apply icons and respect the scriptable asset's layout tinting rules
            if (menuIconDisplay != null)
            {
                menuIconDisplay.sprite = fish.icon;
                menuIconDisplay.color = fish.tint; 
            }

            // Calculate active restaurant pricing curves
            if (goldPayoutText != null)
            {
                // Dave the Diver rewards premium returns for processed dining orders over raw fish dumping values
                int totalValue = Mathf.RoundToInt(fish.baseValue * 1.6f);
                goldPayoutText.text = $"+{totalValue} Gold";
            }
        }

        /// <summary>
        /// Hook this method to an OnClick() event handler on a Button component located on the root of your UI Prefab.
        /// </summary>
       // Change this line in OrderSlipUI.cs:
public void ClickServeRecipeButton(int ignoreThis = 0)

        {
            if (liveInventoryReference == null || targetedFish == null) return;

            // Check if player still owns at least 1 count of this specific fish
            if (liveInventoryReference.CountOf(targetedFish) > 0)
            {
                // Cleanly remove exactly 1 matching index out of the player's catch bag list
                liveInventoryReference.RemoveFirst(targetedFish);

                // TODO: Integrate with your financial player account manager here using the calculation above
                Debug.Log($"Successfully served {targetedFish.Label} Sushi! 1 count removed from inventory.");

                // Tell the board container manager to wipe this specific visual card from the screen array
                if (runtimeManager != null)
                {
                    runtimeManager.DismissTicket(gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"Cannot fulfill! You do not have any {targetedFish.Label} remaining in your bag.");
            }
        }
    }
}
