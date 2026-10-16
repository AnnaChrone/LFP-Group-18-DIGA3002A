using UnityEngine;
using Sushi.Data;

namespace Sushi.Inventory
{
    /// <summary>
    /// Milestone 2. Cumulative weight cap, borrowed from "Dave the Diver".
    /// Already written so the swap is a drag-and-drop on the Inventory
    /// component rather than a refactor. The slot grid stays as the visual
    /// container, and still acts as a hard limit alongside the weight cap.
    ///
    /// FillFraction and FillLabel report whichever limit (slots or weight) is
    /// closest to being hit, so the fill bar always agrees with IsFull.
    /// </summary>
    [CreateAssetMenu(fileName = "Capacity_Weight", menuName = "Sushi/Capacity Rule/Weight Based")]
    public class WeightCapacityRule : CapacityRule
    {
        [Tooltip("Total carry weight before the day phase ends.")]
        [Min(0.1f)] public float maxWeight = 10f;

        [Tooltip("Weight of the lightest catchable item. Once less than this remains, " +
                 "the inventory counts as full because nothing else can fit.")]
        [Min(0.01f)] public float lightestItemWeight = 0.5f;

        [Tooltip("Unit shown in the fill label.")]
        public string unitSuffix = "kg";

        public override bool CanAccept(Inventory inv, CaughtItem item)
        {
            if (!item.IsValid) return false;
            if (inv.Count >= inv.SlotCount) return false;   // still bounded by visual grid
            return inv.TotalWeight + item.weight <= maxWeight;
        }

        public override bool IsFull(Inventory inv)
        {
            if (inv.Count >= inv.SlotCount) return true;
            return (maxWeight - inv.TotalWeight) < lightestItemWeight;
        }

        public override float FillFraction(Inventory inv)
        {
            // If the inventory is full for any reason, the bar must read 100%.
            if (IsFull(inv)) return 1f;

            return Mathf.Clamp01(Mathf.Max(WeightRatio(inv), SlotRatio(inv)));
        }

        public override string FillLabel(Inventory inv)
        {
            // Show whichever limit is actually the binding one.
            if (SlotRatio(inv) > WeightRatio(inv))
                return $"{inv.Count} / {inv.SlotCount} slots";

            return $"{inv.TotalWeight:0.0} / {maxWeight:0.0} {unitSuffix}";
        }

        private float WeightRatio(Inventory inv)
        {
            return maxWeight <= 0f ? 1f : inv.TotalWeight / maxWeight;
        }

        private float SlotRatio(Inventory inv)
        {
            return inv.SlotCount <= 0 ? 1f : (float)inv.Count / inv.SlotCount;
        }
    }
}