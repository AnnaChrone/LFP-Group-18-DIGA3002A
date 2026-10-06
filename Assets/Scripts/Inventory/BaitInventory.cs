using System;
using System.Collections.Generic;
using UnityEngine;
using Sushi.Data;

namespace Sushi.Inventory
{
    [DisallowMultipleComponent]
    public class BaitInventory : MonoBehaviour
    {
        public static BaitInventory Instance { get; private set; }

        // Tracks bait type asset -> current quantity stacked
        private readonly Dictionary<BaitData, int> baitCounts = new Dictionary<BaitData, int>();

        // Event for 3 UI slots to listen to so they know when to redraw
        public event Action OnBaitInventoryChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>Adds a specific amount of bait to its dedicated stack.</summary>
        public void AddBait(BaitData bait, int amount = 1)
        {
            if (bait == null) return;

            if (baitCounts.ContainsKey(bait))
                baitCounts[bait] += amount;
            else
                baitCounts[bait] = amount;

            OnBaitInventoryChanged?.Invoke();
        }

        /// <summary>Consumes bait when casting. Returns true if successful.</summary>
        public bool TryConsumeBait(BaitData bait, int amount = 1)
        {
            if (bait == null || !baitCounts.ContainsKey(bait) || baitCounts[bait] < amount) 
                return false;

            baitCounts[bait] -= amount;
            OnBaitInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>Returns the current amount of a specific bait held.</summary>
        public int GetBaitCount(BaitData bait)
        {
            if (bait == null) return 0;
            if (bait.ownedFromStart) return int.MaxValue; // ASK: ADDED TO MAKE STANDARD FREE
            return baitCounts.ContainsKey(bait) ? baitCounts[bait] : 0;
        }
    }
}
