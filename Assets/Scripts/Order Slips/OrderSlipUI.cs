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

        private RecipeData assignedRecipe;
        private Inventory.Inventory liveInventoryReference;
        private OrderManager runtimeManager;

        public void InitializeRecipeTicket(RecipeData recipe, Inventory.Inventory inventory, OrderManager manager)
        {
            assignedRecipe = recipe;
            liveInventoryReference = inventory;
            runtimeManager = manager;

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
                
                // Keep it clean if there are no extra ingredient modifiers
                ingredientsListText.text = recipe.requiredIngredients.Count > 0 ? trackingList : "";
            }
        }

        public void ClickServeRecipeButton() //HERE WE GOING TO ALTER IT< RATHER IT WILL ACCEPT AS CURRENT ORDER< BUT THIS LOGIC WILL CHECK THE RIGHT THING WAS MADE
        {
            if (liveInventoryReference == null || assignedRecipe == null) return;

            // 1. FIRST PASS: Verify player actually owns everything needed for the dish
            bool canFulfill = true;

            // Check main fish quantity
            if (liveInventoryReference.CountOf(assignedRecipe.mainFish) <= 0)
            {
                canFulfill = false;
            }

            // Check every sub-ingredient quantity
            foreach (ItemData ingredient in assignedRecipe.requiredIngredients)
            {
                if (liveInventoryReference.CountOf(ingredient) <= 0)
                {
                    canFulfill = false;
                    break;
                }
            }

            // 2. SECOND PASS: If check passes, systematically deduct the ingredients
            if (canFulfill)
            {
                // Deduct primary fish
                liveInventoryReference.RemoveFirst(assignedRecipe.mainFish);

                // Deduct each extra item piece matching asset profiles
                foreach (ItemData ingredient in assignedRecipe.requiredIngredients)
                {
                    liveInventoryReference.RemoveFirst(ingredient);
                }

                // TODO: Link up your financial inventory accounting system balance curves here!
                Debug.Log($"Fulfill: {assignedRecipe.recipeName} served! All components extracted.");

                if (runtimeManager != null)
                {
                    runtimeManager.DismissTicket(gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"Missing ingredients to serve {assignedRecipe.recipeName}! Check your catch bag.");
            }
        }
    }
}
