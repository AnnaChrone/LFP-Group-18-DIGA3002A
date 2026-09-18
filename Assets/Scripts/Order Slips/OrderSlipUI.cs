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

        //tracks whether this ticket has moved past the "waiting to be picked up" stage.
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
            if (acceptButton != null) acceptButton.interactable = true;
        }

        /// <summary>
        /// Hooked up to the ticket's "Accept" button. No inventory changes happen here —
        /// this just tells the OrderManager that this recipe is now the active order,
        /// so Cooking.cs knows what to make and the serve window knows what to check for.
        /// </summary>
        public void ClickAcceptOrderButton()
        {
            if (assignedRecipe == null || IsAccepted) return;

            IsAccepted = true;

            if (acceptButton != null) acceptButton.interactable = false;
            if (statusText != null) statusText.text = "Cooking...";

            if (runtimeManager != null)
            {
                // OrderManager needs a method like this to register the ticket as active,
                // so Cooking.cs and the serve window can both look up "what's being worked on".
                runtimeManager.AcceptOrder(assignedRecipe, gameObject);
            }
        }

        /// <summary>
        /// Called by the serve window (not by a button on this ticket anymore) once the
        /// player has the finished plated sushi in their inventory and hits "Serve".
        /// Returns true if the order was successfully served and the ticket dismissed. - WILL PAY OUT HERE
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
            }

            return true;
        }
    }
}