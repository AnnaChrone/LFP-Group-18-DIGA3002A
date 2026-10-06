using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Sushi.Data;
using Sushi.Inventory;

[Serializable]
public struct StoveButtonEntry
{
    public Button button;
    public RecipeData recipe;
}

[Serializable]
public struct IngredientSourceEntry
{
    public IngredientSource source;
    public ItemData itemData;
}

public class Cooking : MonoBehaviour
{
    [Header("Inventory")]
    public Inventory playerInventory;

    [Header("Order Manager")]
    public OrderManager orderManager;

    [Header("Active Order Display")]
    public GameObject activeOrderPanel;
    public Image activeOrderIcon;
    public TMP_Text activeOrderNameText;
    public TMP_Text activeOrderIngredientsText;

    [Header("Sushi Assembler Slots")]
    [Tooltip("Order: 0 = Top, 1 = Middle, 2 = Bottom")]
    public SushiSlot[] slots;

    [Header("Stove UI")]
    public GameObject stoveUIPanel;

    [Header("Trigger")]
    public string playerTag = "Player";

    [Header("Ingredient Sources (Left Side)")]
    public List<IngredientSourceEntry> ingredientSources = new List<IngredientSourceEntry>();

    [Header("Recipe Book")]
    [Tooltip("Drag all your RecipeData assets here.")]
    public List<RecipeData> allRecipes = new List<RecipeData>();

    [Header("Make Sushi Button")]
    [Tooltip("The button the player presses to finalize the sushi.")]
    public Button makeSushiButton;

    [Tooltip("The Image component on the button that shows the result icon.")]
    public Image makeSushiButtonIcon;

    // Cached recipe currently matching the slot arrangement
    private RecipeData currentMatchedRecipe;

    private void Start()
    {
        if (stoveUIPanel != null) stoveUIPanel.SetActive(false);
        RefreshIngredientSourceStates();
        RefreshActiveOrderDisplay();
        RefreshMatchedRecipe(); // Ensure button starts in a clean state

    }

    private void OnEnable()
    {
        if (playerInventory != null) playerInventory.OnChanged += RefreshIngredientSourceStates;
        if (orderManager != null) orderManager.OnActiveOrderChanged += RefreshActiveOrderDisplay;
    }

    private void OnDisable()
    {
        if (playerInventory != null) playerInventory.OnChanged -= RefreshIngredientSourceStates;
        if (orderManager != null) orderManager.OnActiveOrderChanged -= RefreshActiveOrderDisplay;
    }

    public void RefreshMatchedRecipe()
    {
        //  Read the 3 slots
        ItemData providedTop = (slots.Length > 0 && slots[0] != null) ? slots[0].currentItem : null;
        ItemData providedMid = (slots.Length > 1 && slots[1] != null) ? slots[1].currentItem : null;
        ItemData providedBot = (slots.Length > 2 && slots[2] != null) ? slots[2].currentItem : null;

        //  Default state: no match
        currentMatchedRecipe = null;

        //  Search the recipe book for a match
        if (providedTop != null || providedMid != null || providedBot != null)
        {
            foreach (RecipeData recipe in allRecipes)
            {
                if (recipe == null) continue;

                if (recipe.MatchesSlots(providedTop, providedMid, providedBot))
                {
                    currentMatchedRecipe = recipe;
                    break;
                }
            }
        }

        // 4. Update the button visual
        if (makeSushiButton == null) return;

        if (currentMatchedRecipe != null && currentMatchedRecipe.resultItem != null)
        {
            // Show the sushi icon
            if (makeSushiButtonIcon != null)
            {
                makeSushiButtonIcon.sprite = currentMatchedRecipe.resultItem.icon;
                makeSushiButtonIcon.color = currentMatchedRecipe.resultItem.tint;
                makeSushiButtonIcon.enabled = true;
            }

            // Enable the button if the player has the required non-staple ingredients
            makeSushiButton.interactable = HasIngredientsFor(currentMatchedRecipe);
        }
        else
        {
            // No match: hide the icon and disable the button
            if (makeSushiButtonIcon != null)
            {
                makeSushiButtonIcon.sprite = null;
                makeSushiButtonIcon.enabled = false;
            }

            makeSushiButton.interactable = false;
        }
    }

    /// <summary>
    /// Called by the Make Sushi button. Uses the cached matched recipe.
    /// If you want, this can replace your old CheckRecipe() entirely.
    /// </summary>
    public void MakeMatchedSushi()
    {
        if (currentMatchedRecipe == null)
        {
            Debug.Log("No valid recipe in the slots.");
            return;
        }

        if (!HasIngredientsFor(currentMatchedRecipe))
        {
            UiPrompter.Instance.noIngredients();
            return;
        }

        // Use the existing crafting logic (deducts ingredients, adds result)
        ClickCraftSushiButton(currentMatchedRecipe);

        // Clear the slots (which will also call RefreshMatchedRecipe -> button hides)
        foreach (SushiSlot slot in slots)
        {
            if (slot != null) slot.ClearSlot();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) OpenStoveUI();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) CloseStoveUI();
    }

    public void OpenStoveUI()
    {
        if (stoveUIPanel != null) stoveUIPanel.SetActive(true);
        RefreshIngredientSourceStates();
        RefreshActiveOrderDisplay();
    }

    public void CloseStoveUI()
    {
        resetSlots();
        if (stoveUIPanel != null) stoveUIPanel.SetActive(false);
    }


    public void resetSlots()
    {
        foreach (SushiSlot slot in slots)
        {
            if (slot != null) slot.ClearSlot();
        }
    }
    private void RefreshActiveOrderDisplay()
    {
        RecipeData active = orderManager != null ? orderManager.ActiveRecipe : null;

        if (activeOrderPanel != null) activeOrderPanel.SetActive(active != null);
        if (active == null) return;

        if (activeOrderNameText != null) activeOrderNameText.text = active.recipeName;

        if (activeOrderIcon != null && active.resultItem != null)
        {
            activeOrderIcon.sprite = active.resultItem.icon;
            activeOrderIcon.color = active.resultItem.tint;
        }

        if (activeOrderIngredientsText != null)
        {
            string list = "";
            if (active.slot3BOTTOM != null) list += active.slot3BOTTOM.Label;
            if (active.slot2MIDDLE != null) { if (list.Length > 0) list += ", "; list += active.slot2MIDDLE.Label; }
            if (active.slot1TOP != null) { if (list.Length > 0) list += ", "; list += active.slot1TOP.Label; }

            activeOrderIngredientsText.text = list;
        }
    }

    /// <summary>
    /// Greys out ingredient sources the player has run out of.
    /// Skips infinite staples (Rice), which are always shown.
    /// </summary>
    private void RefreshIngredientSourceStates()
    {
        if (playerInventory == null) return;

        foreach (IngredientSourceEntry entry in ingredientSources)
        {
            if (entry.source == null || entry.itemData == null) continue;

            // Infinite staples (Rice) are always visible
            bool shouldBeActive = entry.itemData.isInfiniteStaple
                || playerInventory.CountOf(entry.itemData) > 0;

            // Make sure the object stays enabled so its layout slot is preserved
            entry.source.gameObject.SetActive(true);

            // Fade the visual via alpha
            Image img = entry.source.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = shouldBeActive ? 1f : 0.3f; // 30% opacity when unavailable
                img.color = c;
            }

            // Also toggle the drag script so it can't be dragged when out of stock
            entry.source.enabled = shouldBeActive;
        }
    }

    public void ClickCraftSushiButton(RecipeData recipe)
    {
        if (playerInventory == null || recipe == null) return;

        if (!HasIngredientsFor(recipe))
        {
            UiPrompter.Instance.noIngredients();
            Debug.LogWarning($"Missing ingredients to cook {recipe.recipeName}!");
            return;
        }

        if (recipe.resultItem == null)
        {
            Debug.LogError($"RecipeData '{recipe.recipeName}' has no resultItem assigned.");
            return;
        }

        // Deduct ingredients, BUT skip infinite staples like Rice
        DeductIngredient(recipe.slot1TOP);
        DeductIngredient(recipe.slot2MIDDLE);
        DeductIngredient(recipe.slot3BOTTOM);

        // Grant the finished sushi
        bool added = playerInventory.TryAdd(recipe.resultItem);
        if (!added)
        {
            // Refund on failure
            RefundIngredient(recipe.slot1TOP);
            RefundIngredient(recipe.slot2MIDDLE);
            RefundIngredient(recipe.slot3BOTTOM);

            Debug.LogError($"Could not place cooked {recipe.resultItem.name} - ingredients restored.");
            return;
        }

        Debug.Log($"Cooked: {recipe.recipeName} -> {recipe.resultItem.name} added to inventory.");
    }

    /// <summary>
    /// Removes one of the item from inventory, unless it's an infinite staple.
    /// </summary>
    private void DeductIngredient(ItemData item)
    {
        if (item == null) return;
        if (item.isInfiniteStaple) return; // Don't remove rice!
        playerInventory.RemoveFirst(item);
    }

    /// <summary>
    /// Adds the item back to inventory, unless it's an infinite staple.
    /// </summary>
    private void RefundIngredient(ItemData item)
    {
        if (item == null) return;
        if (item.isInfiniteStaple) return; // Don't add rice back!
        playerInventory.TryAdd(item);
    }

    /// <summary>
    /// Returns true if the player has all required ingredients for this recipe.
    /// Infinite staples (Rice) are always considered available.
    /// </summary>
    private bool HasIngredientsFor(RecipeData recipe)
    {
        if (recipe.slot1TOP != null && !IsAvailable(recipe.slot1TOP)) return false;
        if (recipe.slot2MIDDLE != null && !IsAvailable(recipe.slot2MIDDLE)) return false;
        if (recipe.slot3BOTTOM != null && !IsAvailable(recipe.slot3BOTTOM)) return false;

        return true;
    }

    /// <summary>
    /// Helper: returns true if the item is either an infinite staple, or the player owns at least one.
    /// </summary>
    private bool IsAvailable(ItemData item)
    {
        if (item == null) return true;
        if (item.isInfiniteStaple) return true; // Always available
        return playerInventory.CountOf(item) > 0;
    }

    /// <summary>
    /// Called by the "Cook" button. Reads the 3 slots, finds a matching recipe, and crafts it.
    /// </summary>
    public void CheckRecipe()
    {
        ItemData providedTop = (slots.Length > 0 && slots[0] != null) ? slots[0].currentItem : null;
        ItemData providedMid = (slots.Length > 1 && slots[1] != null) ? slots[1].currentItem : null;
        ItemData providedBot = (slots.Length > 2 && slots[2] != null) ? slots[2].currentItem : null;

        if (providedTop == null && providedMid == null && providedBot == null)
        {
            Debug.Log("No ingredients in the slots.");
            return;
        }

        foreach (RecipeData recipe in allRecipes)
        {
            if (recipe.MatchesSlots(providedTop, providedMid, providedBot))
            {
                Debug.Log($"Match Found: {recipe.recipeName}!");

                if (!HasIngredientsFor(recipe))
                {
                    UiPrompter.Instance.noIngredients();
                    Debug.LogWarning($"Correct arrangement for {recipe.recipeName}, but missing raw ingredients!");
                    return;
                }

                ClickCraftSushiButton(recipe);

                foreach (SushiSlot slot in slots)
                {
                    if (slot != null) slot.ClearSlot();
                }

                return;
            }
        }

        Debug.Log("No recipe matches the current slot arrangement.");
    }
}