using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Sushi.Data;
using Sushi.Inventory;
using Sushi.UI;
using TMPro;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    [Header("Inventory Hook")]
    public Inventory playerInventory;
    public TextMeshProUGUI coinCounter;
    public int counter;

    [Header("Visual Prefabs & Boards")]
    public Transform orderBoardContainer;
    public GameObject orderSlipPrefab;

    [Header("Recipe Database")]
    [Tooltip("Drag all the Recipe Data assets you have created into this list.")]
    public List<RecipeData> globalRecipeBook = new List<RecipeData>();

    [Header("Timing Loops")]
    public float minTimeBetweenOrders = 4f;
    public float maxTimeBetweenOrders = 10f;
    public int maxConcurrentOrders = 5;

    private List<GameObject> spawnedSlips = new List<GameObject>();

    // NEW: tickets the player has accepted (clicked Accept on), waiting to be cooked/served.
    // Kept separate from spawnedSlips so you can query "what's actively in progress"
    // without touching the full board list.
    private List<GameObject> acceptedSlips = new List<GameObject>();

    /// <summary>Read-only view of accepted tickets, for the serve station to check against inventory.</summary>
    public IReadOnlyList<GameObject> AcceptedTickets => acceptedSlips;

    /// <summary>True while an order is accepted and not yet completed/failed — blocks accepting another.</summary>
    public bool HasActiveOrder => acceptedSlips.Count > 0;

    /// <summary>The recipe of the currently accepted order, or null if none. For UI like the stove panel to display.</summary>
    public RecipeData ActiveRecipe { get; private set; }

    /// <summary>Fired whenever an order is accepted, completed, or failed, so tickets can refresh their Accept button.</summary>
    public event Action OnActiveOrderChanged;

    private Coroutine orderRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        counter = 0;

    }

    public void StartServiceOrders()
    {
        ClearBoard();
        orderRoutine = StartCoroutine(GenerationLoop());
    }

    public void StopServiceOrders()
    {
        if (orderRoutine != null) StopCoroutine(orderRoutine);
        ClearBoard();
    }

    private void ClearBoard()
    {
        foreach (GameObject slip in spawnedSlips)
        {
            if (slip != null) Destroy(slip);
        }
        spawnedSlips.Clear();
        acceptedSlips.Clear();
        ActiveRecipe = null;
    }

    private IEnumerator GenerationLoop()
    {
        while (DayNightCycleManager.Instance.currentState == GameState.RestaurantService)
        {
            float delay = UnityEngine.Random.Range(minTimeBetweenOrders, maxTimeBetweenOrders);
            yield return new WaitForSeconds(delay);

            if (spawnedSlips.Count < maxConcurrentOrders)
            {
                TryCreateTicket();
            }
        }
    }

    private void TryCreateTicket()
    {
        // Guard 1: Check if the inspector reference is missing entirely
        if (playerInventory == null)
        {
            Debug.LogError("OrderManager: Player Inventory reference is missing in the Inspector!");
            return;
        }

        // Guard 2: Check if you forgot to add recipes to your global list
        if (globalRecipeBook == null || globalRecipeBook.Count == 0)
        {
            Debug.LogWarning("OrderManager: Your Global Recipe Book is empty! Add some Recipe Data assets in the Inspector.");
            return;
        }

        // Guard 3: Direct inventory check. If total item count is 0, warn immediately and stop.
        if (playerInventory.Count == 0)
        {
            Debug.LogWarning("OrderManager WARNING: Player inventory is completely empty! No orders can be generated.");
            return;
        }

        // 1. Find all recipes where the player has at least the primary fish in stock
        List<RecipeData> viableRecipes = new List<RecipeData>();

        foreach (RecipeData recipe in globalRecipeBook)
        {
            if (recipe == null) continue; // Safety skip if a slot in the list is empty

            if (recipe.mainFish != null && playerInventory.CountOf(recipe.mainFish) > 0)
            {
                viableRecipes.Add(recipe);
            }
        }

        // 2. If we have recipes in our book, but none match the fish currently held
        if (viableRecipes.Count == 0)
        {
            Debug.LogWarning("OrderManager: The player has items, but none of them match the 'Main Fish' required for your recipes.");
            return;
        }

        // 3. Select a random valid recipe from the possible choices
        RecipeData selectedRecipe = viableRecipes[UnityEngine.Random.Range(0, viableRecipes.Count)];

        // 4. Instantiate Visual UI Elements
        if (orderSlipPrefab == null || orderBoardContainer == null)
        {
            Debug.LogError("OrderManager: Order Slip Prefab or Order Board Container is not assigned!");
            return;
        }

        GameObject newSlip = Instantiate(orderSlipPrefab, orderBoardContainer);
        spawnedSlips.Add(newSlip);

        OrderSlipUI slipUI = newSlip.GetComponent<OrderSlipUI>();
        if (slipUI != null)
        {
            slipUI.InitializeRecipeTicket(selectedRecipe, playerInventory, this);
        }
    }

    /// <summary>
    /// Called from OrderSlipUI when the player clicks "Accept" on a ticket.
    /// Doesn't touch inventory — just marks the ticket as in-progress.
    /// Rejects (returns false) if another order is already active.
    /// </summary>
    public bool AcceptOrder(RecipeData recipe, GameObject slipObj)
    {
        if (slipObj == null) return false;

        if (HasActiveOrder)
        {
            Debug.LogWarning($"OrderManager: Can't accept '{recipe.recipeName}' — an order is already in progress.");
            return false;
        }

        acceptedSlips.Add(slipObj);
        ActiveRecipe = recipe;
        Debug.Log($"Order accepted: {recipe.recipeName}");
        OnActiveOrderChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Called from OrderSlipUI.TryServeOrder() once the plated sushi has been
    /// deducted from inventory. Finishes the ticket the same way DismissTicket does.
    /// </summary>
    public void CompleteOrder(GameObject slipObj)
    {

        acceptedSlips.Remove(slipObj);
        ActiveRecipe = null;
        DismissTicket(slipObj);
        OnActiveOrderChanged?.Invoke();
    }

    /// <summary>
    /// Called when the player serves the WRONG plated dish against this ticket.
    /// Same cleanup as CompleteOrder, but no payout — this is the "failed" outcome
    /// that frees the player up to accept a new order.
    /// </summary>
    public void FailOrder(GameObject slipObj)
    {
        acceptedSlips.Remove(slipObj);
        ActiveRecipe = null;
        DismissTicket(slipObj);
        OnActiveOrderChanged?.Invoke();
    }

    public void DismissTicket(GameObject slipObj)
    {
        if (spawnedSlips.Contains(slipObj))
        {
            spawnedSlips.Remove(slipObj);
            Destroy(slipObj);
        }
    }
}