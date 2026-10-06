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
    private List<GameObject> acceptedSlips = new List<GameObject>();

    public IReadOnlyList<GameObject> AcceptedTickets => acceptedSlips;
    public bool HasActiveOrder => acceptedSlips.Count > 0;
    public RecipeData ActiveRecipe { get; private set; }

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
        // NOTE: If your DayNightCycleManager uses 'currentState' (lowercase), change this back.
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
        if (playerInventory == null)
        {
            Debug.LogError("OrderManager: Player Inventory reference is missing in the Inspector!");
            return;
        }

        if (globalRecipeBook == null || globalRecipeBook.Count == 0)
        {
            Debug.LogWarning("OrderManager: Your Global Recipe Book is empty!");
            return;
        }

        if (playerInventory.Count == 0)
        {
            Debug.LogWarning("OrderManager: Player inventory is completely empty! No orders can be generated.");
            return;
        }

        // Find all recipes where the player has:
        //   (a) at least one NON-STAPLE raw ingredient in stock, OR
        //   (b) the finished plated sushi already in inventory
        List<RecipeData> viableRecipes = new List<RecipeData>();

        foreach (RecipeData recipe in globalRecipeBook)
        {
            if (recipe == null) continue;

            bool isViable = false;

            // (a) Check for raw ingredients
            if (recipe.slot1TOP != null && !recipe.slot1TOP.isInfiniteStaple && playerInventory.CountOf(recipe.slot1TOP) > 0)
                isViable = true;
            else if (recipe.slot2MIDDLE != null && !recipe.slot2MIDDLE.isInfiniteStaple && playerInventory.CountOf(recipe.slot2MIDDLE) > 0)
                isViable = true;
            else if (recipe.slot3BOTTOM != null && !recipe.slot3BOTTOM.isInfiniteStaple && playerInventory.CountOf(recipe.slot3BOTTOM) > 0)
                isViable = true;

            // (b) Check for the finished product
            if (!isViable && recipe.resultItem != null && playerInventory.CountOf(recipe.resultItem) > 0)
                isViable = true;

            if (isViable)
            {
                viableRecipes.Add(recipe);
            }
        }

        if (viableRecipes.Count == 0)
        {
            Debug.LogWarning("OrderManager: Player has items, but none match the recipes. Did you forget to mark Rice as an infinite staple, or add fish to inventory?");
            return;
        }

        RecipeData selectedRecipe = viableRecipes[UnityEngine.Random.Range(0, viableRecipes.Count)];

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
    public bool AcceptOrder(RecipeData recipe, GameObject slipObj)
    {
        if (slipObj == null) return false;

        if (HasActiveOrder)
        {
            Debug.LogWarning($"OrderManager: Can't accept '{recipe.recipeName}' - an order is already in progress.");
            return false;
        }

        acceptedSlips.Add(slipObj);
        ActiveRecipe = recipe;
        Debug.Log($"Order accepted: {recipe.recipeName}");
        OnActiveOrderChanged?.Invoke();
        return true;
    }

    public void CompleteOrder(GameObject slipObj)
    {
        acceptedSlips.Remove(slipObj);
        ActiveRecipe = null;
        DismissTicket(slipObj);
        OnActiveOrderChanged?.Invoke();
    }

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