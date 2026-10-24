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

        int currentDay = 1;
        if (DayNightCycleManager.Instance != null)
        {
            currentDay = DayNightCycleManager.Instance.dayCount;
        }

        foreach (BaitData bait in shopBaitAssets)
        {
            if (bait.ownedFromStart) continue;

            // DAY 1 RULE: Force baseline prices
            if (currentDay <= 1)
            {
                currentDailyPrices[bait] = bait.price;
            }
            else
            {
                // DAY 2+ RULE: Apply random price fluctuations
                float fluctuationPercent = Random.Range(-0.30f, 0.30f);
                overallMarketTrend += fluctuationPercent;

                int priceShift = Mathf.RoundToInt(bait.price * fluctuationPercent);
                int finalPrice = Mathf.Max(10, bait.price + priceShift);

                currentDailyPrices[bait] = finalPrice;
            }
        }

        if (currentDay <= 1)
        {
            if (ownerDialogueText != null)
            {
                ownerDialogueText.text = $"<b>{ownerName}:</b> \"Welcome to the bait shop! Come buy some bait when you are ready for a harder and more valuable catch.\"";
            }
        }
        else
        {
            float averageTrend = overallMarketTrend / (shopBaitAssets.Count > 0 ? shopBaitAssets.Count : 1);
            UpdateOwnerDialogue(averageTrend);
        }

        pricesInitialized = true;
    }


    private void UpdateOwnerDialogue(float marketTrend)
    {
        if (ownerDialogueText == null) return;

        List<string> dialogueOptions = new List<string>();

        if (marketTrend > 0.10f) // Prices went up significantly
        {
            dialogueOptions.Add($"\"Supply lines from the mainland are backed up today, fisher. Had to raise the cost of bait a bit.\"");
            dialogueOptions.Add($"\"Storm out at sea ruined my storage locker. Good bait is hard to come by this morning.\"");
            dialogueOptions.Add($"\"High demand today! The fish are biting, and everyone wants a piece of the action.\"");
        }
        else if (marketTrend < -0.10f) // Prices went down significantly
        {
            dialogueOptions.Add($"\"Had a massive haul of worms and crawling critters yesterday. Passing the savings on to you!\"");
            dialogueOptions.Add($"\"Bait's cheap today, fisher! Clear me out so I can restock the shelves tomorrow.\"");
            dialogueOptions.Add($"\"The wholesale supplier gave me a discount this morning. Enjoy the cheaper rates while they last.\"");
        }
        else // Prices stayed relatively stable
        {
            dialogueOptions.Add($"\"Morning! Got the usual assortment ready for you. Steady prices for steady fishers.\"");
            dialogueOptions.Add($"\"Nothing out of the ordinary today. Grab your gear and catch some fish!\"");
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

    private bool isTooltipActive = false;
    [Header("Tooltip Offset Settings")]
    [Tooltip("Offsets the tooltip from the mouse cursor. X pushes it right, Y pushes it down.")]
    [SerializeField] private Vector2 tooltipMouseOffset = new Vector2(25f, -15f);

    private void Update()
    {
        if (isTooltipActive && tooltipPanel != null && UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            
            tooltipPanel.transform.position = mousePosition + tooltipMouseOffset;
        }
    }

    public void ShowTooltip(BaitData bait, Vector3 slotPosition)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        tooltipPanel.SetActive(true);
        isTooltipActive = true;

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
    }

    public void HideTooltip()
    {
        if (tooltipPanel == null) return;

        tooltipPanel.SetActive(false);
        isTooltipActive = false;
    }

}
