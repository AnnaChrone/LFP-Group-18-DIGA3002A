using System.Collections.Generic;
using UnityEngine;
using Sushi.Data;
using Sushi.Inventory;

public class BaitShopManager : MonoBehaviour
{
    [Header("Shop Settings")]
    [SerializeField] private List<BaitData> shopBaitAssets;
    [SerializeField] private Transform slotContainer;      
    [SerializeField] private GameObject slotPrefab;         

    [Header("Tooltip UI")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMPro.TextMeshProUGUI tooltipText;

    private void Start()
    {
        PopulateShop();
        tooltipPanel.SetActive(false);
    }

    private void PopulateShop()
    {
        foreach (Transform child in slotContainer) Destroy(child.gameObject);

        foreach (BaitData bait in shopBaitAssets)
        {
            if (bait.ownedFromStart) continue; 

            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            BaitShopSlot slotScript = newSlot.GetComponent<BaitShopSlot>();
            slotScript.SetupSlot(bait, this);
        }
    }

    public void TryPurchaseBait(BaitData bait)
    {
        if (OrderManager.Instance == null)
        {
            Debug.LogError("BaitShopManager: Cannot find active OrderManager in the scene layout!");
            return;
        }

        if (OrderManager.Instance.counter >= bait.price)
        {
            OrderManager.Instance.counter -= bait.price;

            if (OrderManager.Instance.coinCounter != null)
            {
                OrderManager.Instance.coinCounter.text = OrderManager.Instance.counter.ToString();
            }

            if (Sushi.Inventory.BaitInventory.Instance != null)
            {
                Sushi.Inventory.BaitInventory.Instance.AddBait(bait, 1);
                Debug.Log($"Successfully spent {bait.price} Gold. Added 1x {bait.displayName}!");
            }
        }
        else
        {
            Debug.LogWarning($"Not enough gold! You have {OrderManager.Instance.counter}G, but this costs {bait.price}G.");
        }
    }

    public void ShowTooltip(BaitData bait, Vector3 slotPosition)
    {
        tooltipPanel.SetActive(true);
        string description = $"<b>{bait.displayName}</b>\nCatches:\n";
        
        HashSet<string> uniqueFishNames = new HashSet<string>();
        foreach (var entry in bait.table)
        {
            if (entry != null && entry.item != null && entry.odds > 0)
            {
                uniqueFishNames.Add($"- {entry.item.name}"); 
            }
        }

        description += uniqueFishNames.Count > 0 ? string.Join("\n", uniqueFishNames) : "- Junk / Nothing";
        tooltipText.text = description;
        tooltipPanel.transform.position = slotPosition + new Vector3(160f, 40f, 0f);
    }

    public void HideTooltip() => tooltipPanel.SetActive(false);
}
