using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Sushi.Data;

namespace Sushi.UI
{
    /// <summary>
    /// Builds and maintains the visual inventory grid.
    /// The inventory is display-only and does not accept player input.
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
        [Tooltip("Shown while the bag is full.")]
        [SerializeField] private GameObject fullBanner;

        [Header("Feedback")]
        [SerializeField] private Color rejectColor = new Color(0.85f, 0.25f, 0.25f);
        [SerializeField] private Color normalMessageColor = Color.white;

        [SerializeField, Min(0.5f)]
        private float messageDuration = 1.6f;

        private readonly List<InventorySlotUI> slots = new List<InventorySlotUI>();
        private Coroutine messageRoutine;

        private void Awake()
        {
            if (inventory == null)
                inventory = FindObjectOfType<Sushi.Inventory.Inventory>();
        }

        private void OnEnable()
        {
            if (inventory == null)
                return;

            inventory.OnChanged += Refresh;
            inventory.OnAddRejected += HandleRejected;
            inventory.OnItemAdded += HandleAdded;
        }

        private void OnDisable()
        {
            if (inventory == null)
                return;

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
                Debug.LogError(
                    "[InventoryUI] Assign inventory, slotPrefab and slotParent in the inspector.",
                    this
                );

                return;
            }

            foreach (var existing in slots)
            {
                if (existing != null)
                    Destroy(existing.gameObject);
            }

            slots.Clear();

            for (int i = 0; i < inventory.SlotCount; i++)
            {
                InventorySlotUI slot = Instantiate(slotPrefab, slotParent);

                slot.name = $"Slot_{i:00}";
                slot.Initialise(i);

                slots.Add(slot);
            }
        }

        // --- Refresh --------------------------------------------------------

        public void Refresh()
        {
            if (inventory == null)
                return;

            for (int i = 0; i < slots.Count; i++)
            {
                ItemData item = inventory.GetItemAt(i);

                slots[i].SetItem(item);
                slots[i].SetSelected(false);
            }

            if (fillBar != null)
                fillBar.Refresh();

            if (fullBanner != null)
                fullBanner.SetActive(inventory.IsFull);
        }

        // --- Feedback -------------------------------------------------------

        private void HandleAdded(Sushi.Inventory.CaughtItem item)
        {
            ShowMessage(
                $"Caught {item.Label}",
                normalMessageColor
            );
        }

        private void HandleRejected(Sushi.Inventory.CaughtItem item)
        {
            ShowMessage(
                "No room - the bag is full",
                rejectColor
            );
        }

        public void ShowMessage(string text, Color color)
        {
            if (messageLabel == null)
                return;

            messageLabel.text = text;
            messageLabel.color = color;

            if (messageRoutine != null)
                StopCoroutine(messageRoutine);

            messageRoutine = StartCoroutine(
                ClearMessageAfterDelay()
            );
        }

        private IEnumerator ClearMessageAfterDelay()
        {
            yield return new WaitForSeconds(messageDuration);

            if (messageLabel != null)
                messageLabel.text = string.Empty;

            messageRoutine = null;
        }
    }
}