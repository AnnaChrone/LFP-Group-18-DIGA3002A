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

        // NEW: tracks whether this ticket has moved past the "waiting to be picked up" stage.
        public bool IsAccepted { get; private set; }
        public RecipeData AssignedRecipe => assignedRecipe;

        public void InitializeRecipeTicket(RecipeData recipe, Inventory.Inventory inventory, OrderManager manager)
        {
            assignedRecipe = recipe;
            liveInventoryReference = inventory;
            runtimeManager = manager;
            IsAccepted = false;

            if (menuTitleText != null) menuTitleText.text = recipe.recipeName;

            // Displays the icon of the main fish as the layout centerpiece
            if (menuIconDisplay != null && recipe.mainFish != null)
            {
                menuIconDisplay.sprite = recipe.mainFish.icon;
                menuIconDisplay.color = recipe.mainFish.tint;
            }

            if (goldPayoutText != null)
            {
                goldPayoutText.text = $"+{recipe.recipeValue} Gold";
            }

            // Build a visual string listing required ingredients underneath the title
            if (ingredientsListText != null)
            {
                string trackingList = "Requires: ";
                for (int i = 0; i < recipe.requiredIngredients.Count; i++)
                {
                    trackingList += recipe.requiredIngredients[i].Label;
                    if (i < recipe.requiredIngredients.Count - 1) trackingList += ", ";
                }

                ingredientsListText.text = recipe.requiredIngredients.Count > 0 ? trackingList : "";
            }

            if (statusText != null) statusText.text = "Awaiting Acceptance";

            // Subscribe once we have a manager reference, so this ticket knows when to
            // grey out its Accept button (another order became active) or re-enable it
            // (the board is free again).
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

        /// <summary>
        /// Hooked up to the ticket's "Accept" button. No inventory changes happen here —
        /// this just tells the OrderManager that this recipe is now the active order,
        /// so Cooking.cs knows what to make and the serve window knows what to check for.
        /// </summary>
        public void ClickAcceptOrderButton()
        {
            if (assignedRecipe == null || IsAccepted) return;

            if (runtimeManager == null || !runtimeManager.AcceptOrder(assignedRecipe, gameObject))
            {
                // Rejected — another order is already active. Board should already show
                // this button as non-interactable, but guard here in case of a stray click.
                if (statusText != null) statusText.text = "Order In Progress...";
                return;
            }

            IsAccepted = true;

            if (acceptButton != null) acceptButton.interactable = false;
            if (statusText != null) statusText.text = "Cooking...";
        }

        /// <summary>
        /// Called by the serve window (not by a button on this ticket anymore) once the
        /// player has the finished plated sushi in their inventory and hits "Serve".
        /// Returns true if the order was successfully served and the ticket dismissed.
        /// </summary>
        public bool TryServeOrder()
        {
            if (!IsAccepted || liveInventoryReference == null || assignedRecipe == null) return false;

            // ASSUMPTION: RecipeData has (or needs) a field for the finished plated item,
            // separate from the raw ingredients — e.g. `assignedRecipe.resultItem`.
            // Swap this out for whatever your RecipeData actually exposes.
            ItemData platedSushi = assignedRecipe.resultItem;

            if (platedSushi == null || liveInventoryReference.CountOf(platedSushi) <= 0)
            {
                Debug.LogWarning($"No plated {assignedRecipe.recipeName} in inventory yet — cook it first!");
                return false;
            }

            liveInventoryReference.RemoveFirst(platedSushi);

            // TODO: Link up your financial inventory accounting system balance curves here!

            Debug.Log($"Served: {assignedRecipe.recipeName}! Order complete.");

            if (runtimeManager != null)
            {
                runtimeManager.CompleteOrder(gameObject);
                runtimeManager.counter = runtimeManager.counter + assignedRecipe.recipeValue;
                runtimeManager.coinCounter.text = runtimeManager.counter.ToString();
            }

            return true;
        }

        /// <summary>
        /// Called by the serve station when TryServeOrder() failed — i.e. the player doesn't
        /// have the correct dish, but might be holding a WRONG plated dish and served it anyway.
        /// If any other recipe's plated item is in the inventory, that counts as a failed
        /// delivery: it's removed and this order is marked failed (no payout), freeing the
        /// board up for the next Accept. Returns true if a failure was resolved this way.
        /// </summary>
        public bool TryFailServeWithWrongDish(System.Collections.Generic.List<RecipeData> allRecipes)
        {
            if (!IsAccepted || liveInventoryReference == null || allRecipes == null) return false;

            foreach (RecipeData otherRecipe in allRecipes)
            {
                if (otherRecipe == null || otherRecipe == assignedRecipe || otherRecipe.resultItem == null) continue;

                if (liveInventoryReference.CountOf(otherRecipe.resultItem) > 0)
                {
                    liveInventoryReference.RemoveFirst(otherRecipe.resultItem);

                    Debug.Log($"Served the wrong dish ({otherRecipe.recipeName}) for order '{assignedRecipe.recipeName}' — order failed.");

                    if (statusText != null) statusText.text = "Failed!";

                    if (runtimeManager != null) runtimeManager.FailOrder(gameObject);
                    return true;
                }
            }

            return false;
        }
    }
}