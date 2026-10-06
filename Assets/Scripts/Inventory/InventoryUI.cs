using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sushi.Data;

namespace Sushi.UI
{
    /// <summary>
    /// One row per distinct item held: icon, count, optional total weight.
    /// Use one instance per tab and set the Filter on each.
    /// Row prefab needs direct children named exactly "Icon" (Image),
    /// "CountLabel" (TMP_Text) and "WeightLabel" (TMP_Text, optional).
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public enum CategoryFilter
        {
            ExcludeSushi, // Inventory tab: fish and ingredients only
            OnlySushi     // Sushi tab: made sushi only
        }

        private const string IconChildName = "Icon";
        private const string CountLabelChildName = "CountLabel";
        private const string WeightLabelChildName = "WeightLabel";

        [Header("Wiring")]
        [SerializeField] private Sushi.Inventory.Inventory inventory;
        [SerializeField] private GameObject rowPrefab;
        [Tooltip("The Content object with a Vertical Layout Group on it.")]
        [SerializeField] private RectTransform rowParent;

        [Header("Filter")]
        [SerializeField] private CategoryFilter filter = CategoryFilter.ExcludeSushi;

        [Header("Display")]
        [SerializeField] private bool showWeight = true;
        [SerializeField] private string weightFormat = "0.0";
        [SerializeField] private string weightUnit = "kg";

        private struct Row
        {
            public GameObject root;
            public Image icon;
            public TMP_Text countLabel;
            public TMP_Text weightLabel;
        }

        private readonly List<Row> pooledRows = new List<Row>();
        private readonly List<ItemData> speciesOrder = new List<ItemData>();
        private readonly Dictionary<ItemData, int> counts = new Dictionary<ItemData, int>();
        private readonly Dictionary<ItemData, float> weights = new Dictionary<ItemData, float>();

        private void Awake()
        {
            if (inventory == null) inventory = FindObjectOfType<Sushi.Inventory.Inventory>();
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.OnChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnChanged -= Refresh;
        }

        public void Refresh()
        {
            if (inventory == null || rowPrefab == null || rowParent == null) return;

            BuildCounts();

            while (pooledRows.Count < speciesOrder.Count)
                pooledRows.Add(SpawnRow());

            for (int i = 0; i < pooledRows.Count; i++)
            {
                bool used = i < speciesOrder.Count;
                if (used)
                {
                    ItemData species = speciesOrder[i];
                    ApplyToRow(pooledRows[i], species, counts[species], weights[species]);
                }
                pooledRows[i].root.SetActive(used);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rowParent);
        }

        private Row SpawnRow()
        {
            GameObject instance = Instantiate(rowPrefab, rowParent);

            Transform iconT = instance.transform.Find(IconChildName);
            Transform countT = instance.transform.Find(CountLabelChildName);
            Transform weightT = instance.transform.Find(WeightLabelChildName);

            if (iconT == null || countT == null)
            {
                Debug.LogError($"[InventoryUI] Row prefab '{rowPrefab.name}' needs children named " +
                               $"'{IconChildName}' and '{CountLabelChildName}'.", rowPrefab);
            }

            return new Row
            {
                root = instance,
                icon = iconT != null ? iconT.GetComponent<Image>() : null,
                countLabel = countT != null ? countT.GetComponent<TMP_Text>() : null,
                weightLabel = weightT != null ? weightT.GetComponent<TMP_Text>() : null
            };
        }

        private void ApplyToRow(Row row, ItemData item, int count, float totalWeight)
        {
            if (row.icon != null)
            {
                bool hasIcon = item != null && item.icon != null;
                row.icon.enabled = hasIcon;
                if (hasIcon)
                {
                    row.icon.sprite = item.icon;
                    row.icon.color = item.tint;
                }
            }

            if (row.countLabel != null) row.countLabel.text = count.ToString();

            if (row.weightLabel != null)
            {
                row.weightLabel.gameObject.SetActive(showWeight);
                if (showWeight)
                    row.weightLabel.text = $"{totalWeight.ToString(weightFormat)} {weightUnit}";
            }
        }

        private bool PassesFilter(ItemData data)
        {
            bool isSushi = data.category == ItemCategory.Sushi;
            return filter == CategoryFilter.OnlySushi ? isSushi : !isSushi;
        }

        private void BuildCounts()
        {
            speciesOrder.Clear();
            counts.Clear();
            weights.Clear();

            IReadOnlyList<Sushi.Inventory.CaughtItem> items = inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemData species = items[i].data;
                if (species == null || !PassesFilter(species)) continue;

                if (!counts.ContainsKey(species))
                {
                    counts[species] = 0;
                    weights[species] = 0f;
                    speciesOrder.Add(species);
                }
                counts[species]++;
                weights[species] += items[i].weight;
            }
        }
    }
}