using System;
using System.Collections.Generic;
using UnityEngine;
using Sushi.Data;
using NUnit.Framework.Interfaces;

namespace Sushi.Inventory
{
    /// <summary>
    /// The player's catch bag. Pure state plus events; it draws nothing and
    /// knows nothing about UI, fishing or the day/night cycle. Those systems
    /// subscribe to it.
    ///
    /// Prototype 1 rule: one catch occupies exactly one slot. When the bag
    /// fills, OnBecameFull fires once, which is what ends the day phase
    /// (GDD section 2: capacity, not a timer, ends fishing).
    /// </summary>
    [DisallowMultipleComponent]
    public class Inventory : MonoBehaviour
    {
        [Header("Capacity")]
        [Tooltip("Number of visual slots. Start at 8 for playtesting; the GDD wants the " +
                 "cap to bite early enough that the night phase feels like a rhythm, not an interruption.")]
        [SerializeField, Min(1)] private int slotCount = 8;

        [Tooltip("Leave empty to default to slot-based behaviour. Drop in a Weight rule " +
                 "asset at Milestone 2 to swap the capacity model.")]
        [SerializeField] private CapacityRule capacityRule;

        private readonly List<CaughtItem> items = new List<CaughtItem>();
        private bool wasFullLastCheck;

        // --- Events -------------------------------------------------------
        /// <summary>Anything changed. UI redraws on this.</summary>
        public event Action OnChanged;
        /// <summary>A catch was successfully taken in. Drives the catch popup and SFX.</summary>
        public event Action<CaughtItem> OnItemAdded;
        /// <summary>A catch left the bag (dropped, sold or consumed by an order).</summary>
        public event Action<CaughtItem> OnItemRemoved;
        /// <summary>An add was refused. Drives the "no room" feedback.</summary>
        public event Action<CaughtItem> OnAddRejected;
        /// <summary>Fires once on the transition from not-full to full. DayNightController listens here.</summary>
        public event Action OnBecameFull;
        /// <summary>Fires once on the transition back from full to not-full.</summary>
        public event Action OnBecameNotFull;

        // --- Read-only state ----------------------------------------------
        public int SlotCount => slotCount;
        public int Count => items.Count;
        public IReadOnlyList<CaughtItem> Items => items;
        public bool IsFull => Rule.IsFull(this);
        public float FillFraction => Rule.FillFraction(this);
        public string FillLabel => Rule.FillLabel(this);

        public float TotalWeight
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < items.Count; i++) total += items[i].weight;
                return total;
            }
        }

        private CapacityRule fallbackRule;
        private CapacityRule Rule
        {
            get
            {
                if (capacityRule != null) return capacityRule;
                if (fallbackRule == null) fallbackRule = ScriptableObject.CreateInstance<SlotCapacityRule>();
                return fallbackRule;
            }
        }

        private void Awake()
        {
            wasFullLastCheck = IsFull;
        }

        // --- Mutations ----------------------------------------------------

        /// <summary>Convenience overload: stores a catch at the item's base weight.</summary>
        public bool TryAdd(ItemData item)
        {
            return TryAdd(new CaughtItem(item));
        }

        /// <summary>
        /// Attempts to store a catch. Returns false and fires OnAddRejected
        /// if there is no room, which is the fishing system's cue to refuse
        /// further casts.
        /// </summary>
        public bool TryAdd(CaughtItem catchToAdd)
        {
            if (!catchToAdd.IsValid) return false;

            if (!Rule.CanAccept(this, catchToAdd))
            {
                OnAddRejected?.Invoke(catchToAdd);
                return false;
            }

            items.Add(catchToAdd);
            OnItemAdded?.Invoke(catchToAdd);
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// Would this catch fit right now? The fishing system checks this
        /// before letting the player cast, so a fish is never lost after a
        /// successful fight.
        /// </summary>
        public bool CanAccept(ItemData item) => Rule.CanAccept(this, new CaughtItem(item));
        public bool CanAccept(CaughtItem catchToAdd) => Rule.CanAccept(this, catchToAdd);

        /// <summary>Removes the catch in a given slot. Used by the drop button and by order fulfilment.</summary>
        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= items.Count) return false;

            CaughtItem removed = items[index];
            items.RemoveAt(index);
            OnItemRemoved?.Invoke(removed);
            RaiseChanged();
            return true;
        }

        /// Removes the first slot holding this item type. Returns false if none is held.
        public bool RemoveFirst(ItemData item)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].data == item) return RemoveAt(i);
            }
            return false;
        }

        /// Removes the heaviest catch held, returning its value. Useful if the
        /// group decides a full bag should shed cargo rather than refuse a fish.
        public bool RemoveLowestValue()
        {
            if (items.Count == 0) return false;

            int worst = 0;
            for (int i = 1; i < items.Count; i++)
            {
                int a = items[i].data != null ? items[i].data.baseValue : 0;
                int b = items[worst].data != null ? items[worst].data.baseValue : 0;
                if (a < b) worst = i;
            }
            return RemoveAt(worst);
        }

        ///Empties the bag. Call this after the night phase sells everything off
        public void Clear()
        {
            if (items.Count == 0) return;

            var copy = new List<CaughtItem>(items);
            items.Clear();
            foreach (var item in copy) OnItemRemoved?.Invoke(item);
            RaiseChanged();
        }

        // Queries used by order matching

        /// The asset in a slot, or null if empty. This is what the UI draws.
        public ItemData GetItemAt(int index)
        {
            return (index >= 0 && index < items.Count) ? items[index].data : null;
        }

        /// The full catch record in a slot, including its weight and quality.
        public CaughtItem GetCatchAt(int index)
        {
            return (index >= 0 && index < items.Count) ? items[index] : CaughtItem.None;
        }

        ///How many of this item are held. Order slips check against this.
        public int CountOf(ItemData item)
        {
            int n = 0;
            for (int i = 0; i < items.Count; i++)
                if (items[i].data == item) n++;
            return n;
        }

        public int CountOfId(string id)
        {
            int n = 0;
            for (int i = 0; i < items.Count; i++)
                if (items[i].data != null && items[i].data.id == id) n++;
            return n;
        }

        ///Total coin value of everything held, for the sell-all button.
        public int TotalValue()
        {
            int total = 0;
            for (int i = 0; i < items.Count; i++)
                if (items[i].data != null) total += items[i].data.baseValue;
            return total;
        }

        //Internals

        private void RaiseChanged()
        {
            OnChanged?.Invoke();

            bool full = IsFull;
            if (full && !wasFullLastCheck) OnBecameFull?.Invoke();
            else if (!full && wasFullLastCheck) OnBecameNotFull?.Invoke();
            wasFullLastCheck = full;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (slotCount < 1) slotCount = 1;
        }
#endif
    }
}