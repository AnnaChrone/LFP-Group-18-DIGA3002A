using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sushi.Data;
using Sushi.Inventory;

namespace Sushi.UI
{
    [DisallowMultipleComponent]
    public class OrderSlipUI : MonoBehaviour
    {
        [Header("Layout Assignments")]
        public TMP_Text menuTitleText;
        public Image menuIconDisplay;
        public TMP_Text goldPayoutText;

        [Tooltip("Kept for legacy prefab compatibility. Not used anymore - ingredients are shown on the cooking panel only.")]
        public TMP_Text ingredientsListText;

        [Header("Status")]
        [Tooltip("Optional: label to flip to 'Cooking...' / 'Ready to Serve' once accepted.")]
        public TMP_Text statusText;
        public Button acceptButton;

        private RecipeData assignedRecipe;
        private Inventory.Inventory liveInventoryReference;
        private OrderManager runtimeManager;

        public bool IsAccepted { get; private set; }
        public RecipeData AssignedRecipe => assignedRecipe;

        public void InitializeRecipeTicket(RecipeData recipe, Inventory.Inventory inventory, OrderManager manager)
        {
            assignedRecipe = recipe;
            liveInventoryReference = inventory;
            runtimeManager = manager;
            IsAccepted = false;

            if (menuTitleText != null) menuTitleText.text = recipe.recipeName;

            // Show the result sushi icon on the ticket
            if (menuIconDisplay != null && recipe.resultItem != null)
            {
                menuIconDisplay.sprite = recipe.resultItem.icon;
                menuIconDisplay.color = recipe.resultItem.tint;
            }

            if (goldPayoutText != null)
            {
                goldPayoutText.text = $"+{recipe.recipeValue} Gold";
            }

            // Hide the ingredients list field if it's still on the prefab
            if (ingredientsListText != null)
            {
                ingredientsListText.text = "";
            }

            if (statusText != null) statusText.text = "Awaiting Acceptance";

            if (runtimeManager != null) runtimeManager.OnActiveOrderChanged += RefreshAcceptButtonInteractable;
            RefreshAcceptButtonInteractable();
        }

        private void OnDestroy()
        {
            if (runtimeManager != null) runtimeManager.OnActiveOrderChanged -= RefreshAcceptButtonInteractable;
        }

        private void RefreshAcceptButtonInteractable()
        {
            if (acceptButton == null) return;
            acceptButton.interactable = !IsAccepted && (runtimeManager == null || !runtimeManager.HasActiveOrder);
        }

        public void ClickAcceptOrderButton()
        {
            if (assignedRecipe == null || IsAccepted) return;

            if (runtimeManager == null || !runtimeManager.AcceptOrder(assignedRecipe, gameObject))
            {
                if (statusText != null) statusText.text = "Order In Progress...";
                return;
            }

            IsAccepted = true;

            if (acceptButton != null) acceptButton.interactable = false;
            if (statusText != null) statusText.text = "Cooking...";
        }

        public bool TryServeOrder()
        {
            if (!IsAccepted || liveInventoryReference == null || assignedRecipe == null) return false;

            ItemData platedSushi = assignedRecipe.resultItem;

            if (platedSushi == null || liveInventoryReference.CountOf(platedSushi) <= 0)
            {
                Debug.LogWarning($"No plated {assignedRecipe.recipeName} in inventory yet - cook it first!");
                return false;
            }

            liveInventoryReference.RemoveFirst(platedSushi);

            UiPrompter.Instance.CorrectOrder();
            Debug.Log($"Served: {assignedRecipe.recipeName}! Order complete.");

            if (runtimeManager != null)
            {
                runtimeManager.CompleteOrder(gameObject);
                runtimeManager.counter = runtimeManager.counter + assignedRecipe.recipeValue;
                runtimeManager.coinCounter.text = runtimeManager.counter.ToString();
            }

            return true;
        }

        public bool TryFailServeWithWrongDish(System.Collections.Generic.List<RecipeData> allRecipes)
        {
            if (!IsAccepted || liveInventoryReference == null || allRecipes == null) return false;

            foreach (RecipeData otherRecipe in allRecipes)
            {
                if (otherRecipe == null || otherRecipe == assignedRecipe || otherRecipe.resultItem == null) continue;

                if (liveInventoryReference.CountOf(otherRecipe.resultItem) > 0)
                {
                    liveInventoryReference.RemoveFirst(otherRecipe.resultItem);

                    Debug.Log($"Served the wrong dish ({otherRecipe.recipeName}) for order '{assignedRecipe.recipeName}' - order failed.");
                    UiPrompter.Instance.IncorrectOrder();
                    if (statusText != null) statusText.text = "Failed!";

                    if (runtimeManager != null) runtimeManager.FailOrder(gameObject);
                    return true;
                }
            }

            return false;
        }
    }
}