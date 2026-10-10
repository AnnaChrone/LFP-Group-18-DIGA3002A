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

    [Header("Counter Plate Spawn Points")]
    [Tooltip("Assign your 5 plate transforms from your scene layout here.")]
    public Transform[] plateSpawnPoints = new Transform[5];

    [Header("Visual Prefabs")]
    [Tooltip("Your updated physical world-space Order Slip Prefab.")]
    public GameObject orderSlipPrefab;

    [Header("Recipe Database")]
    [Tooltip("Drag all the Recipe Data assets you have created into this list.")]
    public List<RecipeData> globalRecipeBook = new List<RecipeData>();

    [Header("Timing Loops")]
    public float minTimeBetweenOrders = 4f;
    public float maxTimeBetweenOrders = 10f;

    // Fixed array tracking active tickets placed on 5 physical plates
    private GameObject[] activePlateSlots = new GameObject[5];
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
        
        for (int i = 0; i < activePlateSlots.Length; i++)
        {
            activePlateSlots[i] = null;
        }
        
        ActiveRecipe = null;
    }

    private IEnumerator GenerationLoop()
    {
        // --- INSTANT SWAP CAPTURE: Instantly spawn the first ticket immediately upon startup ---
        TryCreateTicket();

        while (DayNightCycleManager.Instance.currentState == GameState.RestaurantService)
        {
            float delay = UnityEngine.Random.Range(minTimeBetweenOrders, maxTimeBetweenOrders);
            yield return new WaitForSeconds(delay);

            // Let orders keep accumulating up to 5 on the counter plates
            if (spawnedSlips.Count < 5)
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

        // 1. Locate which of the 5 plates are empty
        List<int> freePlateIndices = new List<int>();
        for (int i = 0; i < activePlateSlots.Length; i++)
        {
            if (activePlateSlots[i] == null && plateSpawnPoints[i] != null)
            {
                freePlateIndices.Add(i);
            }
        }

        if (freePlateIndices.Count == 0) return;

        List<RecipeData> viableRecipes = new List<RecipeData>();

        foreach (RecipeData recipe in globalRecipeBook)
        {
            if (recipe == null) continue;

            bool isViable = false;

            if (recipe.slot1TOP != null && !recipe.slot1TOP.isInfiniteStaple && playerInventory.CountOf(recipe.slot1TOP) > 0)
                isViable = true;
            else if (recipe.slot2MIDDLE != null && !recipe.slot2MIDDLE.isInfiniteStaple && playerInventory.CountOf(recipe.slot2MIDDLE) > 0)
                isViable = true;
            else if (recipe.slot3BOTTOM != null && !recipe.slot3BOTTOM.isInfiniteStaple && playerInventory.CountOf(recipe.slot3BOTTOM) > 0)
                isViable = true;

            if (!isViable && recipe.resultItem != null && playerInventory.CountOf(recipe.resultItem) > 0)
                isViable = true;

            if (isViable)
            {
                viableRecipes.Add(recipe);
            }
        }

        if (viableRecipes.Count == 0)
        {
            Debug.LogWarning("OrderManager: Player has items, but none match the recipes.");
            return;
        }

        if (orderSlipPrefab == null)
        {
            Debug.LogError("OrderManager: Order Slip Prefab is not assigned!");
            return;
        }

        // 3. Selection & World Space instantiation onto the chosen target plate
        int chosenPlateIndex = freePlateIndices[UnityEngine.Random.Range(0, freePlateIndices.Count)];
        RecipeData selectedRecipe = viableRecipes[UnityEngine.Random.Range(0, viableRecipes.Count)];
        Transform spawnTarget = plateSpawnPoints[chosenPlateIndex];

        GameObject newSlip = Instantiate(orderSlipPrefab, spawnTarget.position, Quaternion.identity, spawnTarget);
        spawnedSlips.Add(newSlip);
        activePlateSlots[chosenPlateIndex] = newSlip;

        OrderSlipUI slipUI = newSlip.GetComponent<OrderSlipUI>();
        if (slipUI != null)
        {
            slipUI.InitializeRecipeTicket(selectedRecipe, playerInventory, this, chosenPlateIndex);
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

    public void CancelActiveOrder(GameObject slipObj)
    {
        if (acceptedSlips.Contains(slipObj))
        {
            acceptedSlips.Remove(slipObj);
        }
        
        ActiveRecipe = null;
        Debug.Log("Active order was cancelled. Kitchen is now free to accept a new order.");
        OnActiveOrderChanged?.Invoke();
    }


    public void FreeUpPlateSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < activePlateSlots.Length)
        {
            activePlateSlots[slotIndex] = null;
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
