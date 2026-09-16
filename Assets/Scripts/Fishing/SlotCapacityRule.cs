using UnityEngine;
using Sushi.Data;

namespace Sushi.Inventory
{
    /// <summary>
    /// Prototype 1 / Milestone 1. One fish, one slot, regardless of species.
    /// Borrowed from "Spiritfarer" per GDD section 3.
    /// </summary>
    [CreateAssetMenu(fileName = "Capacity_Slots", menuName = "Sushi/Capacity Rule/Slot Based")]
    public class SlotCapacityRule : CapacityRule
    {
        public override bool CanAccept(Inventory inv, CaughtItem item)
        {
            return item.IsValid && inv.Count < inv.SlotCount;
        }

        public override bool IsFull(Inventory inv)
        {
            return inv.Count >= inv.SlotCount;
        }

        public override float FillFraction(Inventory inv)
        {
            return inv.SlotCount <= 0 ? 1f : Mathf.Clamp01((float)inv.Count / inv.SlotCount);
        }

        public override string FillLabel(Inventory inv)
        {
            return $"{inv.Count} / {inv.SlotCount}";
        }
    }
}