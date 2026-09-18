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

        [Header("Economy")]
        [Tooltip("Fulfillment gold payout for successfully serving this complex dish.")]
        [Min(0)] public int recipeValue = 25;
    }
}
