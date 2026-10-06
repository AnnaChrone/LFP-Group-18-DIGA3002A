using UnityEngine;
using System.Collections.Generic;
using Sushi.Data;

namespace Sushi.Data
{
    [CreateAssetMenu(fileName = "Recipe_New", menuName = "Sushi/Recipe Data")]
    public class RecipeData : ScriptableObject
    {
        [Header("Recipe Settings")]
        [Tooltip("The display name of the dish (e.g., 'Tuna Seaweed Roll').")]
        public string recipeName = "Tuna Roll";

        [Header("3-Slot Ingredients")]
        [Tooltip("TOP SLOT: Leave empty if the recipe only uses 2 ingredients.")]
        public ItemData slot1TOP;

        [Tooltip("MIDDLE SLOT: Leave empty if the recipe only uses 1 ingredient.")]
        public ItemData slot2MIDDLE;

        [Tooltip("BOTTOM SLOT: Always the base (e.g., Rice).")]
        public ItemData slot3BOTTOM;

        [Header("Cooking Output")]
        [Tooltip("The finished, plated sushi item this recipe produces at the stove. " +
                 "This is what gets added to inventory after cooking, and what the " +
                 "serve/delivery station checks for — NOT the raw ingredients above.")]
        public ItemData resultItem;

        [Header("Economy")]
        [Tooltip("Fulfillment gold payout for successfully serving this complex dish.")]
        [Min(0)] public int recipeValue = 25;

        // Helper to check if a recipe matches the given slot inputs.
        // Returns true if all non-null recipe slots match the corresponding provided items.
        public bool MatchesSlots(ItemData provided1, ItemData provided2, ItemData provided3)
        {
            // Check Slot 1 (Top) - only if the recipe requires it
            if (slot1TOP != null && slot1TOP != provided1) return false;

            // Check Slot 2 (Middle) - only if the recipe requires it
            if (slot2MIDDLE != null && slot2MIDDLE != provided2) return false;

            // Check Slot 3 (Bottom) - only if the recipe requires it
            if (slot3BOTTOM != null && slot3BOTTOM != provided3) return false;

            // If all required slots match, it's a valid recipe
            return true;
        }
    }
}