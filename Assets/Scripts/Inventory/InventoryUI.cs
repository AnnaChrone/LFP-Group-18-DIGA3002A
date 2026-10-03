using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sushi.Data;

namespace Sushi.UI
{
    /// <summary>
    /// The Inventory tab's whole display: one row per distinct species
    /// currently held, each showing an icon and a count, e.g. "crab icon, 2".
    /// Replaces the earlier one-cell-per-catch slot grid, matching the
    /// updated sketch. The weight threshold bar lives separately on
    /// InventoryFillBar and is unaffected by this change.
    ///
    /// Row prefab convention: each row prefab must have a child named
    /// exactly "Icon" (an Image) and a child named exactly "CountLabel"
    /// (a TMP_Text). No script is needed on the prefab itself, this class
    /// finds those two children by name when it spawns a row. Keeping that
    /// naming consistent is what lets this stay a single file.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        private const string IconChildName = "Icon";
        private const string CountLabelChildName = "CountLabel";

        [Header("Wiring")]
        [SerializeField] private Sushi.Inventory.Inventory inventory;
        [SerializeField] private GameObject rowPrefab;
        [Tooltip("A RectTransform with a Vertical Layout Group (or Grid Layout Group) on it.")]
        [SerializeField] private RectTransform rowParent;

        private struct Row
        {
            public GameObject root;
            public Image icon;
            public TMP_Text countLabel;
        }

        private readonly List<Row> pooledRows = new List<Row>();

        // Reused every refresh to avoid per-frame allocations.
        private readonly List<ItemData> speciesOrder = new List<ItemData>();
        private readonly Dictionary<ItemData, int> counts = new Dictionary<ItemData, int>();

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
                    ApplyToRow(pooledRows[i], species, counts[species]);
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

            if (iconTransform == null || labelTransform == null)
            {
                Debug.LogError($"[InventoryUI] Row prefab '{rowPrefab.name}' needs a child named " +
                                $"'{IconChildName}' and a child named '{CountLabelChildName}'.", rowPrefab);
            }

            return new Row
            {
                root = instance,
                icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null,
                countLabel = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null
            };
        }

        private void ApplyToRow(Row row, ItemData item, int count)
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
        }

        private void BuildCounts()
        {
            speciesOrder.Clear();
            counts.Clear();

            IReadOnlyList<Sushi.Inventory.CaughtItem> items = inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemData species = items[i].data;
                if (species == null) continue;

                if (!counts.ContainsKey(species))
                {
                    counts[species] = 0;
                    speciesOrder.Add(species);
                }
                counts[species]++;
            }
        }
    }
}