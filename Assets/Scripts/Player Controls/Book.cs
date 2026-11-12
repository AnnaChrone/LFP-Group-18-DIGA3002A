using System.Collections.Generic;
using Sushi.Data;
using Sushi.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Book : MonoBehaviour
{
    public static Book Instance { get; private set; }

    public GameObject BookUI;

    [Header("Tabs")]
    public Sprite fishSelected;
    public Sprite fishUnselected;
    public Sprite sushiSelected;
    public Sprite sushiUnselected;
    public Sprite WinSelected;
    public Sprite WinUnselected;

    public Image Fish;
    public Image Sushi;
    public Image Win;

    [Header("Pages")]
    public GameObject fishPages;
    public GameObject sushiPages;
    public GameObject WinPages;
    public GameObject storedPage;

    [Header("Baits")]
    [Tooltip("Every bait in the game (standard, corn, worm). Each fish shows the bait with the best odds of catching it.")]
    public List<BaitData> allBaits = new List<BaitData>();

    [Header("Recipes")]
    [Tooltip("Every recipe in the game, in the order they should appear in the book.")]
    public List<RecipeData> allRecipes = new List<RecipeData>();
    [Tooltip("Off: every recipe in the list is always shown. On: a recipe only appears after RegisterRecipe() is called for it.")]
    public bool showOnlyDiscoveredRecipes = false;
    public string emptySlotText = "Empty";

    /// <summary>One fish entry on a page. Two of these make up a page.</summary>
    [System.Serializable]
    public class FishEntrySlot
    {
        [Tooltip("Parent object of this entry. Hidden when the slot has no fish.")]
        public GameObject root;
        public Image icon;
        public Image baittype;
        public TextMeshProUGUI baitname;
        public TextMeshProUGUI nameText;
        [Tooltip("Shows the catchable weight range, e.g. '0.5kg - 1.5kg'.")]
        public TextMeshProUGUI weightrange;
    }

    /// <summary>
    /// One sushi recipe entry on a page. Two of these make up a page.
    /// Book slot 1 = BOTTOM of the sushi, slot 3 = TOP.
    /// </summary>
    [System.Serializable]
    public class SushiEntrySlot
    {
        [Tooltip("Parent object of this entry. Hidden when the slot has no recipe.")]
        public GameObject root;
        [Tooltip("The finished sushi.")]
        public Image icon;

        [Header("Slot 1 (bottom)")]
        public Image slot1;
        public TextMeshProUGUI slot1ingred;
        [Header("Slot 2 (middle)")]
        public Image slot2;
        public TextMeshProUGUI slot2ingred;
        [Header("Slot 3 (top)")]
        public Image slot3;
        public TextMeshProUGUI slot3ingred;

        [Header("Text")]
        [Tooltip("Comma separated list, e.g. 'Rice, Salmon, Seaweed'.")]
        public TextMeshProUGUI requiredingredients;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI Price;
    }

    [Header("Fish Page Content (2 entries per page)")]
    public FishEntrySlot leftEntry;
    public FishEntrySlot rightEntry;
    public Button prevButton;
    public Button nextButton;
    [Tooltip("Optional, e.g. 'Page 1 / 3'.")]
    public TextMeshProUGUI pageNumberText;

    [Header("Sushi Page Content (2 entries per page)")]
    public SushiEntrySlot leftEntryS;
    public SushiEntrySlot rightEntryS;
    public Button prevButtonS;
    public Button nextButtonS;
    [Tooltip("Optional, e.g. 'Page 1 / 3'.")]
    public TextMeshProUGUI pageNumberTextS;

    [Header("Win Page Content")]
    [Tooltip("Shows 'Current savings: xxx Coins'.")]
    public TextMeshProUGUI currentcoins;
    [Tooltip("Max value is set to the win threshold automatically.")]
    public Slider CoinMeter;
    [Tooltip("Can't be clicked until savings reach the win threshold.")]
    public Button PayButton;
    [Tooltip("Coins needed before the bank can be repaid.")]
    [Min(1)] public int winCoinThreshold = 200;

    private const int EntriesPerPage = 2;

    // Discovered fish, in the order they were first caught.
    private readonly List<ItemData> discoveredFish = new List<ItemData>();
    private int currentPage = 0;

    // Recipes unlocked via RegisterRecipe (only used if showOnlyDiscoveredRecipes is on).
    private readonly HashSet<RecipeData> discoveredRecipes = new HashSet<RecipeData>();
    private int currentSushiPage = 0;

    private int lastShownCoins = int.MinValue;
    private int lastShownThreshold = int.MinValue;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        RefreshWinPage();
    }

    void Start()
    {
        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);

        if (prevButtonS != null) prevButtonS.onClick.AddListener(PrevSushiPage);
        if (nextButtonS != null) nextButtonS.onClick.AddListener(NextSushiPage);

        RefreshFishPage();
        RefreshSushiPage();
        RefreshWinPage();
    }

    private void Update()
    {
        // Watches OrderManager.counter so it stays correct no matter which script changes the coins.
        int coins = CurrentCoins();
        if (coins != lastShownCoins || winCoinThreshold != lastShownThreshold)
            RefreshWinPage();
    }

    // ---------- Catching ----------

    /// <summary>
    /// Call whenever a fish is caught. Only adds it to the book the first time.
    /// Returns true if it was a new entry.
    /// </summary>
    public bool RegisterCatch(ItemData fish)
    {
        if (fish == null || discoveredFish.Contains(fish)) return false;

        discoveredFish.Add(fish);

        // Jump to the page holding the new entry so it's visible next time the book opens.
        currentPage = (discoveredFish.Count - 1) / EntriesPerPage;
        RefreshFishPage();
        return true;
    }

    public bool HasDiscovered(ItemData fish) => discoveredFish.Contains(fish);

    // ---------- Fish paging ----------

    private int PageCount => Mathf.Max(1, Mathf.CeilToInt(discoveredFish.Count / (float)EntriesPerPage));

    public void NextPage()
    {
        if (currentPage < PageCount - 1)
        {
            currentPage++;
            RefreshFishPage();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshFishPage();
        }
    }

    private void RefreshFishPage()
    {
        currentPage = Mathf.Clamp(currentPage, 0, PageCount - 1);

        int first = currentPage * EntriesPerPage;
        PopulateSlot(leftEntry, first);
        PopulateSlot(rightEntry, first + 1);

        if (prevButton != null) prevButton.interactable = currentPage > 0;
        if (nextButton != null) nextButton.interactable = currentPage < PageCount - 1;

        if (pageNumberText != null)
            pageNumberText.text = $"{currentPage + 1} / {PageCount}";
    }

    private void PopulateSlot(FishEntrySlot slot, int index)
    {
        if (slot == null) return;

        bool hasFish = index >= 0 && index < discoveredFish.Count;

        if (slot.root != null) slot.root.SetActive(hasFish);
        if (!hasFish) return;

        ItemData fish = discoveredFish[index];

        if (slot.icon != null)
        {
            slot.icon.sprite = fish.icon;
            slot.icon.color = fish.tint;
            slot.icon.enabled = fish.icon != null;
        }

        if (slot.nameText != null) slot.nameText.text = fish.Label;

        // Weight range, e.g. "0.5kg - 1.5kg"
        if (slot.weightrange != null)
            slot.weightrange.text = $"{FormatKg(fish.minWeight)} - {FormatKg(fish.maxWeight)}";

        // Best bait for this fish
        BaitData bait = GetBestBaitFor(fish);

        if (slot.baittype != null)
        {
            slot.baittype.sprite = bait != null ? bait.icon : null;
            slot.baittype.enabled = bait != null && bait.icon != null;
        }

        if (slot.baitname != null)
            slot.baitname.text = bait != null ? bait.displayName : "Unknown";
    }

    /// <summary>The bait whose catch table gives this fish the highest percentage.</summary>
    private BaitData GetBestBaitFor(ItemData fish)
    {
        BaitData best = null;
        float bestPercent = 0f;

        foreach (BaitData bait in allBaits)
        {
            if (bait == null) continue;

            float percent = bait.PercentFor(fish);
            if (percent > bestPercent)
            {
                bestPercent = percent;
                best = bait;
            }
        }

        return best;
    }

    private static string FormatKg(float value)
    {
        return value.ToString("0.##") + "kg";
    }

    // ---------- Sushi recipes ----------

    /// <summary>
    /// Optional unlock hook, e.g. call it the first time a recipe is cooked.
    /// Only matters when showOnlyDiscoveredRecipes is on. Returns true if it was new.
    /// </summary>
    public bool RegisterRecipe(RecipeData recipe)
    {
        if (recipe == null || !discoveredRecipes.Add(recipe)) return false;

        int index = VisibleRecipes().IndexOf(recipe);
        if (index >= 0) currentSushiPage = index / EntriesPerPage;

        RefreshSushiPage();
        return true;
    }

    private List<RecipeData> VisibleRecipes()
    {
        var list = new List<RecipeData>();
        foreach (RecipeData r in allRecipes)
        {
            if (r == null) continue;
            if (showOnlyDiscoveredRecipes && !discoveredRecipes.Contains(r)) continue;
            list.Add(r);
        }
        return list;
    }

    public void NextSushiPage()
    {
        int pages = Mathf.Max(1, Mathf.CeilToInt(VisibleRecipes().Count / (float)EntriesPerPage));
        if (currentSushiPage < pages - 1)
        {
            currentSushiPage++;
            RefreshSushiPage();
        }
    }

    public void PrevSushiPage()
    {
        if (currentSushiPage > 0)
        {
            currentSushiPage--;
            RefreshSushiPage();
        }
    }

    private void RefreshSushiPage()
    {
        List<RecipeData> recipes = VisibleRecipes();
        int pages = Mathf.Max(1, Mathf.CeilToInt(recipes.Count / (float)EntriesPerPage));
        currentSushiPage = Mathf.Clamp(currentSushiPage, 0, pages - 1);

        int first = currentSushiPage * EntriesPerPage;
        PopulateSushiSlot(leftEntryS, recipes, first);
        PopulateSushiSlot(rightEntryS, recipes, first + 1);

        if (prevButtonS != null) prevButtonS.interactable = currentSushiPage > 0;
        if (nextButtonS != null) nextButtonS.interactable = currentSushiPage < pages - 1;

        if (pageNumberTextS != null)
            pageNumberTextS.text = $"{currentSushiPage + 1} / {pages}";
    }

    private void PopulateSushiSlot(SushiEntrySlot slot, List<RecipeData> recipes, int index)
    {
        if (slot == null) return;

        bool hasRecipe = index >= 0 && index < recipes.Count;

        if (slot.root != null) slot.root.SetActive(hasRecipe);
        if (!hasRecipe) return;

        RecipeData recipe = recipes[index];

        if (slot.nameText != null) slot.nameText.text = recipe.recipeName;
        if (slot.Price != null) slot.Price.text = $"{recipe.recipeValue} coins";

        // Finished sushi icon
        if (slot.icon != null)
        {
            ItemData result = recipe.resultItem;
            slot.icon.sprite = result != null ? result.icon : null;
            slot.icon.color = result != null ? result.tint : Color.white;
            slot.icon.enabled = result != null && result.icon != null;
        }

        // Book slot 1 = bottom, slot 3 = top (RecipeData names them the other way round)
        ItemData bottom = recipe.slot3BOTTOM;
        ItemData middle = recipe.slot2MIDDLE;
        ItemData top = recipe.slot1TOP;

        SetIngredientSlot(slot.slot1, slot.slot1ingred, bottom, recipe.slot3Weight);
        SetIngredientSlot(slot.slot2, slot.slot2ingred, middle, recipe.slot2Weight);
        SetIngredientSlot(slot.slot3, slot.slot3ingred, top, recipe.slot1Weight);

        // "Rice, Salmon, Seaweed" (bottom to top, skipping empty slots)
        if (slot.requiredingredients != null)
        {
            var names = new List<string>();
            if (bottom != null) names.Add(bottom.Label);
            if (middle != null) names.Add(middle.Label);
            if (top != null) names.Add(top.Label);

            slot.requiredingredients.text = string.Join(", ", names);
        }
    }

    private void SetIngredientSlot(Image image, TextMeshProUGUI label, ItemData item, float requiredWeight)
    {
        if (image != null)
        {
            image.sprite = item != null ? item.icon : null;
            image.color = item != null ? item.tint : Color.white;
            image.enabled = item != null && item.icon != null;
        }

        if (label != null)
        {
            if (item == null)
                label.text = emptySlotText;
            else if (requiredWeight > 0f)
                label.text = $"{item.Label} ({FormatKg(requiredWeight)})";
            else
                label.text = item.Label; // no minimum set, e.g. rice
        }
    }

    // ---------- Win page (coins) ----------

    private int CurrentCoins()
    {
        return OrderManager.Instance != null ? OrderManager.Instance.counter : 0;
    }

    private void RefreshWinPage()
    {
        int coins = CurrentCoins();
        int threshold = Mathf.Max(1, winCoinThreshold);

        lastShownCoins = coins;
        lastShownThreshold = winCoinThreshold;

        if (currentcoins != null)
            currentcoins.text = $"Current savings: {coins} Coins";

        if (CoinMeter != null)
        {
            CoinMeter.wholeNumbers = true;
            CoinMeter.minValue = 0;
            CoinMeter.maxValue = threshold;
            CoinMeter.value = Mathf.Clamp(coins, 0, threshold);
        }

        if (PayButton != null)
            PayButton.interactable = coins >= threshold;
    }

    // ---------- Book UI ----------

    public void toggleBook()
    {
        BookUI.SetActive(!BookUI.activeInHierarchy);
    }

    public void openFishPages()
    {
        storedPage.SetActive(false);
        storedPage = fishPages;
        storedPage.SetActive(true);

        Fish.sprite = fishSelected;
        Sushi.sprite = sushiUnselected;
        Win.sprite = WinUnselected;

        RefreshFishPage();
    }

    public void openSushiPages()
    {
        storedPage.SetActive(false);
        storedPage = sushiPages;
        storedPage.SetActive(true);

        Sushi.sprite = sushiSelected;
        Win.sprite = WinUnselected;
        Fish.sprite = fishUnselected;

        RefreshSushiPage();
    }

    public void openWinPages()
    {
        storedPage.SetActive(false);
        storedPage = WinPages;
        storedPage.SetActive(true);

        Win.sprite = WinSelected;
        Sushi.sprite = sushiUnselected;
        Fish.sprite = fishUnselected;

        RefreshWinPage();
    }
}