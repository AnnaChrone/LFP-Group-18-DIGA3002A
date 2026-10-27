using UnityEngine;
using Sushi.Data;

namespace Sushi.Inventory
{
    /// <summary>
    /// Milestone 2. Cumulative weight cap, borrowed from "Dave the Diver".
    ///
    /// The cap applies to FISH AND INGREDIENTS ONLY (the fishing bag), and it
    /// is WEIGHT ONLY: the number of items is unlimited, so the player can keep
    /// catching seaweed as long as the total stays under maxWeight. The slot
    /// count is no longer a limit. The UI grid just shows placeholder cells.
    ///
    /// Cooked sushi is exempt: it never counts toward the weight limit, and a
    /// full fishing bag never blocks cooking. Sushi has its own optional cap
    /// (maxSushi, 0 = unlimited).
    /// </summary>
    [CreateAssetMenu(fileName = "Capacity_Weight", menuName = "Sushi/Capacity Rule/Weight Based")]
    public class WeightCapacityRule : CapacityRule
    {
        [Header("Fishing Bag (fish + ingredients)")]
        [Tooltip("Total carry weight before the day phase ends.")]
        [Min(0.1f)] public float maxWeight = 10f;

        [Tooltip("Weight of the lightest catchable item. Once less than this remains, " +
                 "the fishing bag counts as full because nothing else can fit.")]
        [Min(0.01f)] public float lightestItemWeight = 0.5f;

        [Tooltip("Unit shown in the fill label.")]
        public string unitSuffix = "kg";

        [Header("Cooked Sushi")]
        [Tooltip("Max cooked sushi the player can hold. 0 = unlimited. " +
                 "Sushi never counts toward the fishing weight limit above.")]
        [Min(0)] public int maxSushi = 0;

        // ---------- Capacity API ----------

        public override bool CanAccept(Inventory inv, CaughtItem item)
        {
            if (!item.IsValid) return false;

            // Sushi has its own rules and ignores the fishing bag entirely.
            if (IsSushi(item))
                return maxSushi <= 0 || SushiCount(inv) < maxSushi;

            // Fish / ingredients: weight is the only limit.
            return FishWeight(inv) + item.weight <= maxWeight;
        }

        public override bool IsFull(Inventory inv)
        {
            // "Full" is about the fishing bag only, so cooked sushi can never
            // push the player into the night phase.
            return (maxWeight - FishWeight(inv)) < lightestItemWeight;
        }

        public override float FillFraction(Inventory inv)
        {
            if (IsFull(inv)) return 1f;
            return Mathf.Clamp01(WeightRatio(inv));
        }

        public override string FillLabel(Inventory inv)
        {
            return $"{FishWeight(inv):0.0} / {maxWeight:0.0} {unitSuffix}";
        }

        // ---------- Helpers (fish only, sushi excluded) ----------

        private static bool IsSushi(CaughtItem item)
        {
            return item.data != null && item.data.category == ItemCategory.Sushi;
        }

        private static int SushiCount(Inventory inv)
        {
            int n = 0;
            var items = inv.Items;
            for (int i = 0; i < items.Count; i++)
                if (items[i].IsValid && IsSushi(items[i])) n++;
            return n;
        }

        private static float FishWeight(Inventory inv)
        {
            float w = 0f;
            var items = inv.Items;
            for (int i = 0; i < items.Count; i++)
                if (items[i].IsValid && !IsSushi(items[i])) w += items[i].weight;
            return w;
        }

        private float WeightRatio(Inventory inv)
        {
            return maxWeight <= 0f ? 1f : FishWeight(inv) / maxWeight;
        }
    }
}