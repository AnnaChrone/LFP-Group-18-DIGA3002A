using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
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
[Serializable]
public struct StoveButtonEntry
{
    public Button button;
    public RecipeData recipe;
}

public class Cooking : MonoBehaviour
{
    [Header("Inventory Hook")]
    public Inventory playerInventory;

    [Header("Order Manager Hook")]
    public OrderManager orderManager;

    [Header("Active Order Display")]
    [Tooltip("Shown/hidden based on whether an order is currently accepted.")]
    public GameObject activeOrderPanel;
    public Image activeOrderIcon;
    public TMP_Text activeOrderNameText;
    public TMP_Text activeOrderIngredientsText;

    [Header("Craftable Sushi")]
    [Tooltip("Each button on the stove UI paired with the recipe it cooks. " +
             "Drag the Salmon/Tuna/Crab/Prawn/Seaweed buttons and their matching Recipe Data here " +
             "so they can be greyed out when the player can't afford them.")]
    public List<StoveButtonEntry> stoveButtons = new List<StoveButtonEntry>();

    [Header("Stove UI")]
    [Tooltip("The panel that pops up with the chef + sushi buttons when the player approaches.")]
    public GameObject stoveUIPanel;

    [Header("Trigger")]
    [Tooltip("Tag the player object must have for the stove UI to open on approach.")]
    public string playerTag = "Player";

    private void Start()
    {
        if (stoveUIPanel != null) stoveUIPanel.SetActive(false);
        RefreshButtonStates();
        RefreshActiveOrderDisplay();
    }

    private void OnEnable()
    {
        if (playerInventory != null) playerInventory.OnChanged += RefreshButtonStates;
        if (orderManager != null) orderManager.OnActiveOrderChanged += RefreshActiveOrderDisplay;
    }

    private void OnDisable()
    {
        if (playerInventory != null) playerInventory.OnChanged -= RefreshButtonStates;
        if (orderManager != null) orderManager.OnActiveOrderChanged -= RefreshActiveOrderDisplay;
    }

    // 3D trigger. If your stove/player use 2D colliders, swap these for
    // OnTriggerEnter2D(Collider2D other) / OnTriggerExit2D(Collider2D other).
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) OpenStoveUI();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) CloseStoveUI();
    }

    public void OpenStoveUI()
    {
        if (stoveUIPanel != null) stoveUIPanel.SetActive(true);
        RefreshButtonStates();
        RefreshActiveOrderDisplay();
    }

    public void CloseStoveUI()
    {
        if (stoveUIPanel != null) stoveUIPanel.SetActive(false);
    }

    /// <summary>
    /// Shows the currently accepted order's name/icon/ingredients on the stove panel so the
    /// player can see what they need to make without leaving the stove. Hides the panel
    /// entirely when there's no active order. Runs whenever OrderManager.OnActiveOrderChanged fires.
    /// </summary>
    private void RefreshActiveOrderDisplay()
    {
        RecipeData active = orderManager != null ? orderManager.ActiveRecipe : null;

        if (activeOrderPanel != null) activeOrderPanel.SetActive(active != null);
        if (active == null) return;

        if (activeOrderNameText != null) activeOrderNameText.text = active.recipeName;

        if (activeOrderIcon != null && active.mainFish != null)
        {
            activeOrderIcon.sprite = active.mainFish.icon;
            activeOrderIcon.color = active.mainFish.tint;
        }

        if (activeOrderIngredientsText != null)
        {
            string list = active.mainFish != null ? active.mainFish.Label : "";
            for (int i = 0; i < active.requiredIngredients.Count; i++)
            {
                if (list.Length > 0) list += ", ";
                list += active.requiredIngredients[i].Label;
            }
            activeOrderIngredientsText.text = list;
        }
    }

    /// <summary>
    /// Greys out any button whose recipe the player can't currently afford,
    /// re-run automatically whenever the inventory changes.
    /// </summary>
    private void RefreshButtonStates()
    {
        foreach (StoveButtonEntry entry in stoveButtons)
        {
            if (entry.button == null || entry.recipe == null) continue;
            entry.button.interactable = HasIngredientsFor(entry.recipe);
        }
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