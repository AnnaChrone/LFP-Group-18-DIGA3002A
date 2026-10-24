using System.Collections.Generic;
using UnityEngine;
using TMPro;
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
    [SerializeField] private TextMeshProUGUI tooltipText;

    [Header("Shop Owner Dialogue UI")]
    [SerializeField] private TextMeshProUGUI ownerDialogueText;
    [SerializeField] private string ownerName = "Shop Owner Suki";

    private Dictionary<BaitData, int> currentDailyPrices = new Dictionary<BaitData, int>();
    
    private bool pricesInitialized = false;

    private void Start()
    {
        if (!pricesInitialized)
        {
            GenerateNewDailyPrices();
        }
        tooltipPanel.SetActive(false);
    }

    private void OnEnable()
    {

        PopulateShop();
    }

    public void OnNewDayStarted()
    {
        GenerateNewDailyPrices();
    }

    private void GenerateNewDailyPrices()
    {
        currentDailyPrices.Clear();
        float overallMarketTrend = 0f;

        foreach (BaitData bait in shopBaitAssets)
        {
            if (bait.ownedFromStart) continue;

            float fluctuationPercent = Random.Range(-0.30f, 0.30f);
            overallMarketTrend += fluctuationPercent;

            int priceShift = Mathf.RoundToInt(bait.price * fluctuationPercent);
            int finalPrice = Mathf.Max(10, bait.price + priceShift);

            currentDailyPrices[bait] = finalPrice;
        }

        // Average out the fluctuations across the inventory to pick a contextual dialogue line
        float averageTrend = overallMarketTrend / (shopBaitAssets.Count > 0 ? shopBaitAssets.Count : 1);
        UpdateOwnerDialogue(averageTrend);

        pricesInitialized = true;
    }

    private void UpdateOwnerDialogue(float marketTrend)
    {
        if (ownerDialogueText == null) return;

        List<string> dialogueOptions = new List<string>();

        if (marketTrend > 0.10f) // Prices went up significantly
        {
            dialogueOptions.Add($"\"Supply lines from the mainland are backed up today, mate. Had to raise the cost of bait a bit.\"");
            dialogueOptions.Add($"\"Storm out at sea ruined my storage locker. Good bait is hard to come by this morning.\"");
            dialogueOptions.Add($"\"High demand this season! The fish are biting, and everyone wants a piece of the action.\"");
        }
        else if (marketTrend < -0.10f) // Prices went down significantly
        {
            dialogueOptions.Add($"\"Had a massive haul of worms and crawling critters yesterday. Passing the savings on to you!\"");
            dialogueOptions.Add($"\"Bait's cheap today, fisherman! Clear me out so I can restock the shelves tomorrow.\"");
            dialogueOptions.Add($"\"The wholesale supplier gave me a discount this morning. Enjoy the cheaper rates while they last.\"");
        }
        else // Prices stayed relatively stable
        {
            dialogueOptions.Add($"\"Morning! Got the usual assortment ready for you. Steady prices for steady fishers.\"");
            dialogueOptions.Add($"\"Nothing out of the ordinary today. Grab your gear and get out on the water!\"");
        }

        // Pick a random line from the appropriate market category
        string chosenLine = dialogueOptions[Random.Range(0, dialogueOptions.Count)];
        ownerDialogueText.text = $"<b>{ownerName}:</b> {chosenLine}";
    }

    private void PopulateShop()
    {
        foreach (Transform child in slotContainer) Destroy(child.gameObject);

        if (currentDailyPrices.Count == 0)
        {
            GenerateNewDailyPrices();
        }

        foreach (BaitData bait in shopBaitAssets)
        {
            if (bait.ownedFromStart) continue; 

            // If a new bait was added to the script lists mid-game, generate a safety fallback price
            if (!currentDailyPrices.ContainsKey(bait)) 
            {
                currentDailyPrices[bait] = bait.price;
            }

            GameObject newSlot = Instantiate(slotPrefab, slotContainer);
            BaitShopSlot slotScript = newSlot.GetComponent<BaitShopSlot>();
            
            int priceForToday = currentDailyPrices[bait];
            slotScript.SetupSlotWithCustomPrice(bait, priceForToday, this);
        }
    }

    public void TryPurchaseBait(BaitData bait)
    {
        if (OrderManager.Instance == null) return;

        int activePrice = currentDailyPrices.ContainsKey(bait) ? currentDailyPrices[bait] : bait.price;

        if (OrderManager.Instance.counter >= activePrice)
        {
            OrderManager.Instance.counter -= activePrice;

            if (OrderManager.Instance.coinCounter != null)
            {
                OrderManager.Instance.coinCounter.text = OrderManager.Instance.counter.ToString();
            }

            if (Sushi.Inventory.BaitInventory.Instance != null)
            {
                Sushi.Inventory.BaitInventory.Instance.AddBait(bait, 1);
            }
        }
        else
        {
            Debug.LogWarning("Not enough gold for today's price rates!");
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
                  uniqueFishNames.Add($"- {entry.item.displayName}"); 
            }
        }

        description += uniqueFishNames.Count > 0 ? string.Join("\n", uniqueFishNames) : "- Junk / Nothing";
        tooltipText.text = description;
        tooltipPanel.transform.position = slotPosition + new Vector3(160f, 40f, 0f);
    }

    public void HideTooltip() => tooltipPanel.SetActive(false);
}
