using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class FloatEvent : UnityEvent<float> { }

[Serializable]
public class QoLUpgrade
{
    [Header("Shop info")]
    public string id;               // unique, e.g. "bigger_bag". Used for save/purchased tracking
    public string displayName;
    [TextArea(2, 4)] public string description;
    public int price;
    public Sprite icon;             // optional

    [Header("Effect")]
    [Tooltip("Passed to the function(s) below when the upgrade is bought.")]
    public float value = 1f;
    [Tooltip("Drag in the object + function that does the upgrade. Pick the 'Dynamic float' version so it receives Value above.")]
    public FloatEvent onPurchased;

    [Header("Rules")]
    [Tooltip("If true, can be bought repeatedly (e.g. stacking bag slots) up to maxLevel.")]
    public bool repeatable = false;
    public int maxLevel = 1;
    public int priceIncrease = 2;

    [NonSerialized] public int level; // how many times bought

    public bool IsMaxed => level >= (repeatable ? maxLevel : 1);
}

// Holds the master list of upgrades and handles buying them.
public class QoLManager : MonoBehaviour
{
    public static QoLManager Instance { get; private set; }

    public List<QoLUpgrade> upgrades = new List<QoLUpgrade>();

    public event Action<QoLUpgrade> OnUpgradePurchased;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool CanAfford(QoLUpgrade u) => GetPlayerCoins() >= u.price;

    /// <summary>Tries to buy. Returns true if it went through.</summary>
    public bool TryPurchase(QoLUpgrade u)
    {
        if (u.IsMaxed || !CanAfford(u)) return false;

        SpendPlayerCoins(u.price);
        u.level++;
        u.onPurchased?.Invoke(u.value);   // <-- runs the upgrade's function
        OnUpgradePurchased?.Invoke(u);
        return true;
    }

    // ------------------------------------------------------------------
    // HOOK THESE UP TO YOUR EXISTING COIN SYSTEM
    // Replace the bodies with however your game stores/spends coins.
    // ------------------------------------------------------------------
    int GetPlayerCoins()
    {
        return OrderManager.Instance.counter;
    }

    void SpendPlayerCoins(int amount)
    {
        OrderManager.Instance.counter -= amount;
    }

    //Upgrade functions
    public void AcceptMultipleOrders(float amount)
    {
        // e.g. Inventory.Instance.maxSlots += (int)amount;
        Debug.Log($"Bag slots +{amount}");
    }

    public void IncreaseInventory(float percent)
    {
        // e.g. FishingSystem.Instance.resistanceMultiplier *= 1f - percent / 100f;
        Debug.Log($"Reel resistance -{percent}%");
    }

    public void AddServer(float amount)
    {
        // e.g. Inventory.Instance.maxSlots += (int)amount;
        Debug.Log($"Added new server +{amount}");
    }

    public void UnlockRecipes()
    {
        //Unlock sushi recipes here
       
    }
}

