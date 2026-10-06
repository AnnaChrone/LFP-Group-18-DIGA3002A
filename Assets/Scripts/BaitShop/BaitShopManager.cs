using System.Collections.Generic;
using UnityEngine;
using Sushi.Data;

public class BaitShopManager : MonoBehaviour
{
    [Header("Shop Settings")]
    [SerializeField] private List<BaitData> shopBaitAssets; // Assign your BaitData ScriptableObjects here
    [SerializeField] private Transform slotContainer;       // VerticalLayoutGroup/Content of ScrollView
    [SerializeField] private GameObject slotPrefab;          // UI Row template with BaitShopSlot attached

    [Header("Tooltip UI")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMPro.TextMeshProUGUI tooltipText;

    // TODO: Link this to your real Player Wallet/Economy script
    private int playerGold = 500; 

    private void Start()
    {
        PopulateShop();
        tooltipPanel.SetActive(false);
    }

    private void PopulateShop()
    {
        // Clear existing UI elements
        foreach (Transform child in slotContainer) Destroy(child.gameObject);

        // Build shop inventory from BaitData assets
        foreach (BaitData bait in shopBaitAssets)
        {
            // Skip free/starting bait if you don't want them polluting the shop window
            if (bait.ownedFromStart) continue; 

            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            BaitShopSlot slotScript = newSlot.GetComponent<BaitShopSlot>();
            slotScript.SetupSlot(bait, this);
        }
    }

    public void TryPurchaseBait(BaitData bait)
{
    if (playerGold >= bait.price)
    {
        playerGold -= bait.price;
        Debug.Log($"Purchased {bait.displayName}! Remaining Gold: {playerGold}");
        
        // HOOK INTO NEW SYSTEM: Add 1 bait to the player's dedicated bait slots
        if (Sushi.Inventory.BaitInventory.Instance != null)
        {
            Sushi.Inventory.BaitInventory.Instance.AddBait(bait, 1);
        }
    }
    else
    {
        Debug.Log("Not enough gold!");
    }
}


    public void ShowTooltip(BaitData bait, Vector3 slotPosition)
    {
        tooltipPanel.SetActive(true);
        
        // Dynamically build description using the ItemData inside the Catch Table
        string description = $"<b>{bait.displayName}</b>\nCatches:\n";
        
        HashSet<string> uniqueFishNames = new HashSet<string>();
        foreach (var entry in bait.table)
        {
            if (entry != null && entry.item != null && entry.odds > 0)
            {
                // If ItemData has a display name field, use that instead of .name
                uniqueFishNames.Add($"- {entry.item.name}"); 
            }
        }

        if (uniqueFishNames.Count > 0)
        {
            description += string.Join("\n", uniqueFishNames);
        }
        else
        {
            description += "- Junk / Nothing";
        }

        tooltipText.text = description;
        
        // Shift tooltip panel relative to slot container placement
        tooltipPanel.transform.position = slotPosition + new Vector3(160f, 40f, 0f);
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }
}
