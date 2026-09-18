using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sushi.Data;
using Sushi.Inventory;
using Sushi.UI;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

       [Header("Inventory Hook")]
    // TO THIS:
    public Inventory playerInventory; 


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
    private Coroutine orderRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
    }

    private IEnumerator GenerationLoop()
    {
        while (DayNightCycleManager.Instance.currentState == GameState.RestaurantService)
        {
            float delay = Random.Range(minTimeBetweenOrders, maxTimeBetweenOrders);
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
    RecipeData selectedRecipe = viableRecipes[Random.Range(0, viableRecipes.Count)];

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


    public void DismissTicket(GameObject slipObj)
    {
        if (spawnedSlips.Contains(slipObj))
        {
            spawnedSlips.Remove(slipObj);
            Destroy(slipObj);
        }
    }
}
