using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sushi.Data;

namespace Sushi.UI
{
    /// <summary>
    /// The Inventory tab's whole display: one row per distinct species
    /// currently held, each showing an icon, a count, and the total weight.
    ///
    /// Row prefab convention: each row prefab needs children named exactly
    /// "Icon" (Image), "CountLabel" (TMP_Text) and "WeightLabel" (TMP_Text).
    /// WeightLabel is optional; if it's missing, the weight is just not shown.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        private const string IconChildName = "Icon";
        private const string CountLabelChildName = "CountLabel";
        private const string WeightLabelChildName = "WeightLabel";

        [Header("Wiring")]
        [SerializeField] private Sushi.Inventory.Inventory inventory;
        [SerializeField] private GameObject rowPrefab;
        [Tooltip("A RectTransform with a Vertical Layout Group on it.")]
        [SerializeField] private RectTransform rowParent;

        [Header("Display")]
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

        // Reused every refresh to avoid per-frame allocations.
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
                if (i < speciesOrder.Count)
                {
                    ItemData species = speciesOrder[i];
                    ApplyToRow(pooledRows[i], species, counts[species], weights[species]);
                    pooledRows[i].root.SetActive(true);
                }
                else
                {
                    pooledRows[i].root.SetActive(false);
                }
            }
        }

        private Row SpawnRow()
        {
            GameObject instance = Instantiate(rowPrefab, rowParent);

            Transform iconTransform = instance.transform.Find(IconChildName);
            Transform labelTransform = instance.transform.Find(CountLabelChildName);
            Transform weightTransform = instance.transform.Find(WeightLabelChildName);

            if (iconTransform == null || labelTransform == null)
            {
                Debug.LogError($"[InventoryUI] Row prefab '{rowPrefab.name}' needs a child named " +
                               $"'{IconChildName}' and a child named '{CountLabelChildName}'.", rowPrefab);
            }

            return new Row
            {
                root = instance,
                icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null,
                countLabel = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null,
                weightLabel = weightTransform != null ? weightTransform.GetComponent<TMP_Text>() : null
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
                row.weightLabel.text = $"{totalWeight.ToString(weightFormat)} {weightUnit}";
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
                if (species == null) continue;

                if (!counts.ContainsKey(species))
                {
                    counts[species] = 0;
                    weights[species] = 0f;
                    speciesOrder.Add(species);
                }
                counts[species]++;
                weights[species] += GetWeight(items[i]);
            }
        }

        // CHANGE THIS LINE if your weight field is named differently,
        // or lives on ItemData (e.g. return catchItem.data.weight;).
        private static float GetWeight(Sushi.Inventory.CaughtItem catchItem)
        {
            return catchItem.weight;
        }
    }
}