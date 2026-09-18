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

        [Tooltip("The main fish required for this recipe.")]
        public ItemData mainFish;

        [Header("Additional Ingredients")]
        [Tooltip("List of extra ingredients required (e.g., Seaweed, Rice, Wasabi).")]
        public List<ItemData> requiredIngredients = new List<ItemData>();

        [Header("Cooking Output")]
        [Tooltip("The finished, plated sushi item this recipe produces at the stove. " +
                 "This is what gets added to inventory after cooking, and what the " +
                 "serve/delivery station checks for — NOT the raw ingredients above.")]
        public ItemData resultItem;

        [Header("Economy")]
        [Tooltip("Fulfillment gold payout for successfully serving this complex dish.")]
        [Min(0)] public int recipeValue = 25;
    }
}