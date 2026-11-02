using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Sushi.Data;
using Sushi.Inventory;
using Sushi.UI;
using TMPro;

public enum BudgetTier { Budget, Standard, VIP }

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    [Header("Inventory Hook")]
    public Inventory playerInventory;
    public TextMeshProUGUI coinCounter;
    public int counter; //Coins

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

    [Header("Customer Balancing")]
    [Tooltip("Percentage chance (0-100) that a customer is a cheap Budget tier.")]
    [Range(0f, 100f)] public float budgetChance = 25f;
    [Tooltip("Percentage chance (0-100) that a customer is a high-paying VIP tier.")]
    [Range(0f, 100f)] public float vipChance = 15f;

    private GameObject[] activePlateSlots = new GameObject[5];
    private List<GameObject> spawnedSlips = new List<GameObject>();
    private List<GameObject> acceptedSlips = new List<GameObject>();

    public IReadOnlyList<GameObject> AcceptedTickets => acceptedSlips;
    public bool HasActiveOrder => acceptedSlips.Count > 0;
    [Header("Order Limits")]
    [Tooltip("How many orders can be accepted at once. The QoL upgrade raises this.")]
    [Min(1)] public int maxActiveOrders = 1;

    private readonly List<RecipeData> activeRecipes = new List<RecipeData>();
    public IReadOnlyList<RecipeData> ActiveRecipes => activeRecipes;
    public RecipeData ActiveRecipe => activeRecipes.Count > 0 ? activeRecipes[0] : null;

    public event Action OnActiveOrderChanged;

    private Coroutine orderRoutine;

    [Header("Recipe Unlocks")]
    public bool upgradeRecipesUnlocked = false;

    public bool IsRecipeAvailable(RecipeData recipe)
    {
        if (recipe == null) return false;
        return !recipe.upgradeRecipe || upgradeRecipesUnlocked;
    }

    public void UnlockUpgradeRecipes()
    {
        upgradeRecipesUnlocked = true;
        Debug.Log("Upgrade recipes unlocked!");
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        counter = 0;
        upgradeRecipesUnlocked = false;
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

        activeRecipes.Clear();
        OnActiveOrderChanged?.Invoke();
    }

    private void RebuildActiveRecipes()
    {
        activeRecipes.Clear();
        foreach (GameObject slip in acceptedSlips)
        {
            if (slip == null) continue;
            OrderSlipUI ui = slip.GetComponent<OrderSlipUI>();
            if (ui != null && ui.AssignedRecipe != null) activeRecipes.Add(ui.AssignedRecipe);
        }
    }
    private IEnumerator GenerationLoop()
    {

        TryCreateTicket();

        while (DayNightCycleManager.Instance.currentState == GameState.RestaurantService)
        {
            float delay = UnityEngine.Random.Range(minTimeBetweenOrders, maxTimeBetweenOrders);
            yield return new WaitForSeconds(delay);

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
            if (!IsRecipeAvailable(recipe)) continue;   // replaces the null check

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

        int chosenPlateIndex = freePlateIndices[UnityEngine.Random.Range(0, freePlateIndices.Count)];
        RecipeData selectedRecipe = viableRecipes[UnityEngine.Random.Range(0, viableRecipes.Count)];
        Transform spawnTarget = plateSpawnPoints[chosenPlateIndex];

        BudgetTier chosenCustomerTier = BudgetTier.Standard;
        float roll = UnityEngine.Random.Range(0f, 100f);

        if (roll < budgetChance)
        {
            chosenCustomerTier = BudgetTier.Budget;
        }
        else if (roll >= (100f - vipChance))
        {
            chosenCustomerTier = BudgetTier.VIP;
        }

        GameObject newSlip = Instantiate(orderSlipPrefab, spawnTarget.position, Quaternion.identity, spawnTarget);
        spawnedSlips.Add(newSlip);
        activePlateSlots[chosenPlateIndex] = newSlip;

        OrderSlipUI slipUI = newSlip.GetComponent<OrderSlipUI>();
        if (slipUI != null)
        {
            slipUI.InitializeRecipeTicket(selectedRecipe, playerInventory, this, chosenPlateIndex, chosenCustomerTier);
        }
    }

    public bool AcceptOrder(RecipeData recipe, GameObject slipObj)
    {
        if (slipObj == null || acceptedSlips.Contains(slipObj)) return false;

        if (acceptedSlips.Count >= maxActiveOrders)
        {
            Debug.LogWarning($"OrderManager: Can't accept '{recipe.recipeName}' - already at {maxActiveOrders} active order(s).");
            return false;
        }

        acceptedSlips.Add(slipObj);
        RebuildActiveRecipes();
        Debug.Log($"Order accepted: {recipe.recipeName}");
        OnActiveOrderChanged?.Invoke();
        return true;
    }

    public void CompleteOrder(GameObject slipObj)
    {
        acceptedSlips.Remove(slipObj);
        DismissTicket(slipObj);
        RebuildActiveRecipes();
        OnActiveOrderChanged?.Invoke();
    }

    public void FailOrder(GameObject slipObj)
    {
        acceptedSlips.Remove(slipObj);
        DismissTicket(slipObj);
        RebuildActiveRecipes();
        OnActiveOrderChanged?.Invoke();
    }



    public void CancelActiveOrder(GameObject slipObj)
    {
        acceptedSlips.Remove(slipObj);

        OrderSlipUI slip = slipObj.GetComponent<OrderSlipUI>();
        if (slip != null)
            NightSummaryManager.Instance.IncreaseCancellationDeductions(slip.currentGoldPayout);

        RebuildActiveRecipes();
        Debug.Log("Active order was cancelled.");
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
