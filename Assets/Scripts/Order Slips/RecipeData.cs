using UnityEngine;
using System.Collections.Generic;
using Sushi.Data;

namespace Sushi.Data
{
    [CreateAssetMenu(fileName = "Recipe_New", menuName = "Sushi/Recipe Data")]
    public class RecipeData : ScriptableObject
    {
        [Header("Recipe Settings")]
        public string recipeName = "Tuna Roll";

        [Header("3-Slot Ingredients")]
        [Tooltip("TOP SLOT: Leave empty if the recipe only uses 2 ingredients.")]
        public ItemData slot1TOP;

        [Tooltip("Minimum total weight of slot1TOP required. Ignored if slot1TOP is null.")]
        [Min(0f)] public float slot1Weight = 0f;

        [Tooltip("MIDDLE SLOT: Leave empty if the recipe only uses 1 ingredient.")]
        public ItemData slot2MIDDLE;

        [Tooltip("Minimum total weight of slot2MIDDLE required. Ignored if slot2MIDDLE is null.")]
        [Min(0f)] public float slot2Weight = 0f;

        [Tooltip("BOTTOM SLOT: Always the base (e.g., Rice).")]
        public ItemData slot3BOTTOM;

        [Tooltip("Minimum total weight of slot3BOTTOM required. Ignored if slot3BOTTOM is null.")]
        [Min(0f)] public float slot3Weight = 0f;

        [Header("Cooking Output")]
        public ItemData resultItem;

        [Header("Economy")]
        [Min(0)] public int recipeValue = 25;

        public bool MatchesSlots(ItemData provided1, ItemData provided2, ItemData provided3)
        {
            if (slot1TOP != null && slot1TOP != provided1) return false;
            if (slot2MIDDLE != null && slot2MIDDLE != provided2) return false;
            if (slot3BOTTOM != null && slot3BOTTOM != provided3) return false;
            return true;
        }
    }
}