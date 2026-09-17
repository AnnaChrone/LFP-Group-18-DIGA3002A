using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sushi.Data;
using Sushi.Inventory; // Connects to your Inventory namespace
using Sushi.UI;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    [Header("Inventory Hook")]
    [Tooltip("Drag the Player's Inventory component here.")]
    public Inventory playerInventory;

    [Header("Visual Prefabs & Boards")]
    [Tooltip("The UI layout component parent that handles sorting on the wall.")]
    public Transform orderBoardContainer;
    [Tooltip("The Ticket Prefab UI gameobject asset.")]
    public GameObject orderSlipPrefab;

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
        // Keeps checking if the active loop is still running your service shift
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
        if (playerInventory == null)
        {
            Debug.LogError("OrderManager: No Player Inventory reference assigned!");
            return;
        }

        // 1. Parse your real inventory items list to extract uniquely available Fish types
        List<ItemData> validFishInBag = new List<ItemData>();
        
        for (int i = 0; i < playerInventory.Items.Count; i++)
        {
            ItemData fish = playerInventory.Items[i].data;
            if (fish != null && fish.category == ItemCategory.Fish)
            {
                // Simple duplicate exclusion tracker
                if (!validFishInBag.Contains(fish))
                {
                    validFishInBag.Add(fish);
                }
            }
        }

        // 2. Halt generation if player is out of stock entirely
        if (validFishInBag.Count == 0)
        {
            Debug.LogWarning("No fish available in player's catch bag to generate orders!");
            return;
        }

        // 3. Pick a random choice from the gathered collection
        ItemData selectedFish = validFishInBag[Random.Range(0, validFishInBag.Count)];

        // 4. Instantiate Visual UI Elements
        GameObject newSlip = Instantiate(orderSlipPrefab, orderBoardContainer);
        spawnedSlips.Add(newSlip);

        OrderSlipUI slipUI = newSlip.GetComponent<OrderSlipUI>();
        if (slipUI != null)
        {
            slipUI.InitializeTicket(selectedFish, playerInventory, this);
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
