using System.Collections.Generic;
using Sushi.Data;
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

    [Header("Fish Page Content (2 entries per page)")]
    public FishEntrySlot leftEntry;
    public FishEntrySlot rightEntry;
    public Button prevButton;
    public Button nextButton;
    [Tooltip("Optional, e.g. 'Page 1 / 3'.")]
    public TextMeshProUGUI pageNumberText;

    [Header("Sushi Page Content")]
    public Image sushiIcon;

    [Header("Win Page Content")]
    public Image Fill;
    public Button PayoutButton;

    private const int EntriesPerPage = 2;

    // Discovered fish, in the order they were first caught.
    private readonly List<ItemData> discoveredFish = new List<ItemData>();
    private int currentPage = 0;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);

        RefreshFishPage();
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

    // ---------- Paging ----------

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
    }

    public void openWinPages()
    {
        storedPage.SetActive(false);
        storedPage = WinPages;
        storedPage.SetActive(true);

        Win.sprite = WinSelected;
        Sushi.sprite = sushiUnselected;
        Fish.sprite = fishUnselected;
    }
}