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

        [Tooltip("Optional: A text sub-field to show extra requested item list strings.")]
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

            // We skip infinite staples so the ticket shows the actual fish, not rice.
          /*  ItemData heroItem = null;
            if (recipe.slot3 != null && !recipe.slot3.isInfiniteStaple) heroItem = recipe.slot1;
            else if (recipe.slot2 != null && !recipe.slot2.isInfiniteStaple) heroItem = recipe.slot2;
            else if (recipe.slot1 != null && !recipe.slot1.isInfiniteStaple) heroItem = recipe.slot3;

            // Fallback: if the recipe is somehow only rice, use whatever exists
            if (heroItem == null) heroItem = recipe.slot3 ?? recipe.slot2 ?? recipe.slot1;*/

            if (menuIconDisplay != null )
            {
                menuIconDisplay.sprite = recipe.resultItem.icon;
            }

            if (goldPayoutText != null)
            {
                goldPayoutText.text = $"+{recipe.recipeValue} Gold";
            }

            // Build a visual string listing required ingredients, ordered bottom-to-top.
            if (ingredientsListText != null)
            {
                string trackingList = "";

                if (recipe.slot3BOTTOM != null)
                {
                    trackingList += recipe.slot3BOTTOM.Label;
                    // Optional: mark staples clearly
                    // if (recipe.slot3.isInfiniteStaple) trackingList += " (free)";
                }
                if (recipe.slot2MIDDLE != null)
                {
                    if (trackingList.Length > 0) trackingList += ", ";
                    trackingList += recipe.slot2MIDDLE.Label;
                    // if (recipe.slot2.isInfiniteStaple) trackingList += " (free)";
                }
                if (recipe.slot1TOP != null)
                {
                    if (trackingList.Length > 0) trackingList += ", ";
                    trackingList += recipe.slot1TOP.Label;
                    // if (recipe.slot1.isInfiniteStaple) trackingList += " (free)";
                }

                ingredientsListText.text = trackingList.Length > 0 ? "Requires: " + trackingList : "";
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