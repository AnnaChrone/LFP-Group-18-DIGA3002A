using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sushi.Data;

namespace Sushi.UI
{
    /// <summary>
    /// Builds and maintains the slot grid. Spawns one InventorySlotUI per
    /// slot at startup and redraws the whole grid whenever the inventory
    /// raises OnChanged. At eight slots a full redraw is far cheaper than
    /// tracking diffs, and it cannot drift out of sync.
    ///
    /// Selection is kept here rather than in Inventory, because "which slot
    /// is highlighted" is a UI concern. Milestone 2's sushi assembly screen
    /// reads SelectedItem when the player commits an ingredient.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Sushi.Inventory.Inventory inventory;
        [SerializeField] private InventorySlotUI slotPrefab;
        [Tooltip("A RectTransform with a GridLayoutGroup on it.")]
        [SerializeField] private RectTransform slotParent;

        [Header("Readouts (optional)")]
        [SerializeField] private InventoryFillBar fillBar;
        [SerializeField] private TMP_Text itemNameLabel;
        [SerializeField] private TMP_Text messageLabel;

        [Header("Full Banner (optional)")]
        [Tooltip("Shown while the bag is full. Communicates the pacing beat before the phase flips.")]
        [SerializeField] private GameObject fullBanner;

        [Header("Behaviour")]
        [Tooltip("Right-clicking a slot discards the item.")]
        [SerializeField] private bool allowRightClickDrop = true;
        [SerializeField, Min(0.5f)] private float messageDuration = 1.6f;

        [Header("Feedback")]
        [SerializeField] private Color rejectColor = new Color(0.85f, 0.25f, 0.25f);
        [SerializeField] private Color normalMessageColor = Color.white;

        private readonly List<InventorySlotUI> slots = new List<InventorySlotUI>();
        private int selectedIndex = -1;
        private Coroutine messageRoutine;

        /// <summary>Raised when the player picks a slot. Order fulfilment listens here.</summary>
        public event Action<int, ItemData> OnSelectionChanged;

        public ItemData SelectedItem => inventory != null ? inventory.GetItemAt(selectedIndex) : null;
        public int SelectedIndex => selectedIndex;

        private void Awake()
        {
            if (inventory == null) inventory = FindObjectOfType<Sushi.Inventory.Inventory>();
        }

        private void OnEnable()
        {
            if (inventory == null) return;
            inventory.OnChanged += Refresh;
            inventory.OnAddRejected += HandleRejected;
            inventory.OnItemAdded += HandleAdded;
        }

        private void OnDisable()
        {
            if (inventory == null) return;
            inventory.OnChanged -= Refresh;
            inventory.OnAddRejected -= HandleRejected;
            inventory.OnItemAdded -= HandleAdded;
        }

        private void Start()
        {
            BuildGrid();
            Refresh();
        }

        // --- Construction ---------------------------------------------------

        private void BuildGrid()
        {
            if (slotPrefab == null || slotParent == null || inventory == null)
            {
                Debug.LogError("[InventoryUI] Assign inventory, slotPrefab and slotParent in the inspector.", this);
                return;
            }

            foreach (var existing in slots)
                if (existing != null) Destroy(existing.gameObject);
            slots.Clear();

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                InventorySlotUI slot = Instantiate(slotPrefab, slotParent);
                slot.name = $"Slot_{i:00}";
                slot.Initialise(i);
                slot.OnClicked += HandleSlotClicked;
                slot.OnRightClicked += HandleSlotRightClicked;
                slot.OnHovered += HandleSlotHovered;
                slots.Add(slot);
            }
        }

        // --- Refresh --------------------------------------------------------

        public void Refresh()
        {
            if (inventory == null) return;

            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].SetItem(inventory.GetItemAt(i));
                slots[i].SetSelected(i == selectedIndex && inventory.GetItemAt(i) != null);
            }

            // A selected slot can be emptied by an order or a drop.
            if (selectedIndex >= 0 && inventory.GetItemAt(selectedIndex) == null)
                SetSelected(-1);

            if (fillBar != null) fillBar.Refresh();
            if (fullBanner != null) fullBanner.SetActive(inventory.IsFull);
        }

        // --- Input handlers --------------------------------------------------

        private void HandleSlotClicked(int index)
        {
            if (inventory.GetItemAt(index) == null)
            {
                SetSelected(-1);
                return;
            }
            SetSelected(index == selectedIndex ? -1 : index);
        }

        private void HandleSlotRightClicked(int index)
        {
            if (!allowRightClickDrop) return;

            ItemData item = inventory.GetItemAt(index);
            if (item == null) return;

            inventory.RemoveAt(index);
            ShowMessage($"Dropped {item.Label}", normalMessageColor);
        }

        private void HandleSlotHovered(ItemData item)
        {
            if (itemNameLabel == null) return;
            itemNameLabel.text = item != null ? item.Label : string.Empty;
        }

        /// <summary>Hook this to an on-screen Drop button for touch or gamepad.</summary>
        public void DropSelected()
        {
            if (selectedIndex < 0) return;
            HandleSlotRightClicked(selectedIndex);
        }

        private void SetSelected(int index)
        {
            selectedIndex = index;
            for (int i = 0; i < slots.Count; i++)
                slots[i].SetSelected(i == selectedIndex);

            OnSelectionChanged?.Invoke(selectedIndex, SelectedItem);
        }

        // --- Feedback ---------------------------------------------------------

        private void HandleAdded(Sushi.Inventory.CaughtItem item)
        {
            ShowMessage($"Caught {item.Label}", normalMessageColor);
        }

        private void HandleRejected(Sushi.Inventory.CaughtItem item)
        {
            ShowMessage("No room — the bag is full", rejectColor);
        }

        public void ShowMessage(string text, Color color)
        {
            if (messageLabel == null) return;

            messageLabel.text = text;
            messageLabel.color = color;

            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(ClearMessageAfterDelay());
        }

        private IEnumerator ClearMessageAfterDelay()
        {
            yield return new WaitForSeconds(messageDuration);
            if (messageLabel != null) messageLabel.text = string.Empty;
            messageRoutine = null;
        }
    }
}