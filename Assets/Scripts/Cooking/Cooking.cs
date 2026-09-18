using UnityEngine;
using System.Collections.Generic;
using Sushi.Data;
using Sushi.Inventory;

// Milestone 1 flow:
// Player walks to the stove -> a UI pops up with buttons for the different sushi types
// (plus their current order slip for reference). Clicking a button consumes the raw
// ingredients for that recipe and grants the finished, plated sushi item.
// Player then carries that plated sushi to the delivery/serve station, where it's
// checked against the accepted order (see OrderSlipUI.TryServeOrder).
//
// NOTE: cooking here is intentionally NOT restricted to "only the accepted recipe" —
// matching against an order.happens *at the serve station*, not at the stove, so the
// player can cook the wrong thing and lose ingredients for it (per your delivery notes).
public class Cooking : MonoBehaviour
{
    [Header("Inventory Hook")]
    public Inventory playerInventory;

    [Header("Craftable Sushi")]
    [Tooltip("The recipes this stove can cook. Usually the same list as OrderManager's globalRecipeBook.")]
    public List<RecipeData> craftableRecipes = new List<RecipeData>();

    [Header("Stove UI")]
    [Tooltip("The panel that pops up with the chef + sushi buttons when the player approaches.")]
    public GameObject stoveUIPanel;

    private void Start()
    {
        if (stoveUIPanel != null) stoveUIPanel.SetActive(false);
    }



    // Hook this up to your "walk into stove trigger" logic.
    public void OpenStoveUI()
    {
        if (stoveUIPanel != null) stoveUIPanel.SetActive(true);
    }

    public void CloseStoveUI()
    {
        if (stoveUIPanel != null) stoveUIPanel.SetActive(false);
    }

    /// <summary>
    /// Wire this to each sushi button in the stove UI, one per RecipeData
    /// (e.g. button.onClick.AddListener(() => ClickCraftSushiButton(recipe));).
    /// </summary>
    public void ClickCraftSushiButton(RecipeData recipe)
    {
        if (playerInventory == null || recipe == null) return;

        if (!HasIngredientsFor(recipe))
        {
            Debug.LogWarning($"Missing ingredients to cook {recipe.recipeName}! Check your catch bag.");
            return;
        }

        if (recipe.resultItem == null)
        {
            Debug.LogError($"RecipeData '{recipe.recipeName}' has no resultItem assigned — cooking aborted so ingredients aren't wasted.");
            return;
        }

        // Check there's room for the plated dish BEFORE deducting ingredients, so a full
        // bag doesn't eat the player's raw ingredients for nothing.
        if (!playerInventory.CanAccept(recipe.resultItem))
        {
            Debug.LogWarning($"No room in the catch bag for {recipe.resultItem.name}! Free up a slot first.");
            return;
        }

        // Deduct raw ingredients
        playerInventory.RemoveFirst(recipe.mainFish);
        foreach (ItemData ingredient in recipe.requiredIngredients)
        {
            playerInventory.RemoveFirst(ingredient);
        }

        // Grant the finished, plated sushi
        playerInventory.TryAdd(recipe.resultItem);
        Debug.Log($"Cooked: {recipe.recipeName} -> {recipe.resultItem.name} added to inventory.");
    }

    private bool HasIngredientsFor(RecipeData recipe)
    {
        if (playerInventory.CountOf(recipe.mainFish) <= 0) return false;

        foreach (ItemData ingredient in recipe.requiredIngredients)
        {
            if (playerInventory.CountOf(ingredient) <= 0) return false;
        }

        return true;
    }
}