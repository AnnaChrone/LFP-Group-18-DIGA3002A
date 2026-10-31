using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;
using Sushi.Data;

namespace Sushi.UI
{
    /// <summary>
    /// Grid of slots. One cell per item held, so it reads as slot-based even
    /// though the real limit is weight (see WeightCapacityRule).
    /// Use one instance per tab and set the Filter on each.
    ///
    /// Slot prefab: a root Image (the cell background) with these direct children:
    ///   "Icon"        (Image, required)
    ///   "WeightLabel" (TMP_Text, optional: that item's weight)
    ///   "Locked"      (GameObject, optional: overlay shown on empty cells once the bag is full)
    /// The slot parent needs a Grid Layout Group.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public enum CategoryFilter
        {
            ExcludeSushi, // Inventory tab: fish and ingredients only
            OnlySushi     // Sushi tab: made sushi only
        }

        private const string IconChildName = "Icon";
        private const string WeightLabelChildName = "WeightLabel";
        private const string LockedChildName = "Locked";

        [Header("Wiring")]
        [SerializeField] private Sushi.Inventory.Inventory inventory;
        [FormerlySerializedAs("rowPrefab")]
        [SerializeField] private GameObject slotPrefab;
        [FormerlySerializedAs("rowParent")]
        [Tooltip("The Content object with a Grid Layout Group on it.")]
        [SerializeField] private RectTransform slotParent;

        [Header("Filter")]
        [SerializeField] private CategoryFilter filter = CategoryFilter.ExcludeSushi;

        [Header("Placeholder Slots")]
        [Tooltip("Fish tab: how many empty cells to show. These are just placeholders, not a limit. " +
                 "The grid grows past this as more items are caught.")]
        [SerializeField, Min(1)] private int fishPlaceholderSlots = 8;

        [Tooltip("Sushi tab: minimum number of cells to show. Grows if more sushi is held.")]
        [SerializeField, Min(1)] private int sushiMinSlots = 12;

        [Header("Display")]
        [SerializeField] private bool showWeight = true;
        [SerializeField] private string weightFormat = "0.0";
        [SerializeField] private string weightUnit = "kg";

        private struct Slot
        {
            public GameObject root;
            public Image icon;
            public TMP_Text weightLabel;
            public GameObject locked;
        }

        private readonly List<Slot> pooledSlots = new List<Slot>();
        private struct Stack
        {
            public ItemData data;
            public int count;
            public float weight;   // total weight of the stack
        }

        private readonly List<Stack> shown = new List<Stack>();

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
            if (inventory == null || slotPrefab == null || slotParent == null) return;

            BuildShownList();

            bool fishTab = filter == CategoryFilter.ExcludeSushi;
            int placeholders = fishTab ? fishPlaceholderSlots : sushiMinSlots;
            int cellCount = Mathf.Max(placeholders, shown.Count); // never hide a held item

            while (pooledSlots.Count < cellCount)
                pooledSlots.Add(SpawnSlot());

            // Once the fishing bag is full (weight or slots), empty cells get a
            // locked look so unused cells don't suggest there is still room.
            bool lockEmpty = fishTab && inventory.IsFull;

            for (int i = 0; i < pooledSlots.Count; i++)
            {
                bool active = i < cellCount;
                pooledSlots[i].root.SetActive(active);
                if (!active) continue;

                if (i < shown.Count) ApplyFilled(pooledSlots[i], shown[i]);
                else ApplyEmpty(pooledSlots[i], lockEmpty);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(slotParent);
        }

        private Slot SpawnSlot()
        {
            GameObject instance = Instantiate(slotPrefab, slotParent);

            Transform iconT = instance.transform.Find(IconChildName);
            Transform weightT = instance.transform.Find(WeightLabelChildName);
            Transform lockedT = instance.transform.Find(LockedChildName);

            if (iconT == null)
            {
                Debug.LogError($"[InventoryUI] Slot prefab '{slotPrefab.name}' needs a child named " +
                               $"'{IconChildName}'.", slotPrefab);
            }

            return new Slot
            {
                root = instance,
                icon = iconT != null ? iconT.GetComponent<Image>() : null,
                weightLabel = weightT != null ? weightT.GetComponent<TMP_Text>() : null,
                locked = lockedT != null ? lockedT.gameObject : null
            };
        }

        private void ApplyFilled(Slot slot, Stack stack)
        {
            ItemData data = stack.data;

            if (slot.icon != null)
            {
                bool hasIcon = data != null && data.icon != null;
                slot.icon.enabled = hasIcon;
                if (hasIcon)
                {
                    slot.icon.sprite = data.icon;
                    slot.icon.color = data.tint;
                }
            }

            if (slot.weightLabel != null)
            {
                bool show = showWeight || stack.count > 1;
                slot.weightLabel.gameObject.SetActive(show);
                if (show)
                {
                    string w = $"{stack.weight.ToString(weightFormat)} {weightUnit}";
                    slot.weightLabel.text = w;
                }
            }

            if (slot.locked != null) slot.locked.SetActive(false);
        }

        private void ApplyEmpty(Slot slot, bool locked)
        {
            if (slot.icon != null) slot.icon.enabled = false;
            if (slot.weightLabel != null) slot.weightLabel.gameObject.SetActive(false);
            if (slot.locked != null) slot.locked.SetActive(locked);
        }

        private bool PassesFilter(ItemData data)
        {
            bool isSushi = data.category == ItemCategory.Sushi;
            return filter == CategoryFilter.OnlySushi ? isSushi : !isSushi;
        }

        private void BuildShownList()
        {
            shown.Clear();

            IReadOnlyList<Sushi.Inventory.CaughtItem> items = inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemData data = items[i].data;
                if (data == null || !PassesFilter(data)) continue;

                int found = -1;
                for (int s = 0; s < shown.Count; s++)
                    if (shown[s].data == data) { found = s; break; }

                if (found < 0)
                {
                    shown.Add(new Stack { data = data, count = 1, weight = items[i].weight });
                }
                else
                {
                    Stack st = shown[found];
                    st.count++;
                    st.weight += items[i].weight;
                    shown[found] = st;
                }
            }
        }
    }
}