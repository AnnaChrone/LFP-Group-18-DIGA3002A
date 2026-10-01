using UnityEngine;

namespace Sushi.Data
{
    public enum ItemCategory
    {
        Fish,
        Ingredient,
        Sushi
    }

    /// <summary>
    /// One catchable or usable thing: tuna, salmon, prawn, crab, seaweed.
    /// (GDD Goal 4: Build Modular Systems.)
    /// </summary>
    [CreateAssetMenu(fileName = "Item_New", menuName = "Sushi/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable lowercase key used by order-matching logic. Never rename this once orders reference it.")]
        public string id = "tuna";

        [Tooltip("Shown in the UI and on order slips.")]
        public string displayName = "Tuna";

        public ItemCategory category = ItemCategory.Fish;

        [Header("Presentation")]
        public Sprite icon;

        [Tooltip("Multiplied over the icon. Leave white unless you are recolouring a shared silhouette.")]
        public Color tint = Color.white;

        [Header("Economy")]
        [Tooltip("Coins earned when this is sold raw. Order fulfilment pays separately.")]
        [Min(0)] public int baseValue = 10;

        [Header("Capacity")]
        [Tooltip("Milestone 2 only. Ignored by SlotCapacityRule, read by WeightCapacityRule. " +
                 "Set sensible values now so the swap needs no data pass later.")]
        [Min(0.01f)] public float weight = 1f;

        [Tooltip("Weight of the worst possible catch of this species (sloppy fight).")]
        [Min(0f)] public float minWeight = 0.5f;

        [Tooltip("Weight of the best possible catch of this species (near-perfect fight).")]
        [Min(0f)] public float maxWeight = 1.5f;

        [Header("Fishing")]
        [Tooltip("Lowest resistance this item can have during a fight. 0 = offers no resistance (seaweed).")]
        [Range(0f, 1f)] public float minResistance = 0.2f;

        [Tooltip("Highest resistance this item can reach during a fight. 1 = hardest to reel (salmon, tuna).")]
        [Range(0f, 1f)] public float maxResistance = 0.6f;

        /// <summary>Safe display name even if the field was left blank in the asset.</summary>
        public string Label => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        public float WeightForProficiency(float proficiency01)
        {
            if (minWeight <= 0f && maxWeight <= 0f) return weight;
            return Mathf.Lerp(minWeight, maxWeight, Mathf.Clamp01(proficiency01));
        }

        private void OnValidate()
        {
            if (maxResistance < minResistance) maxResistance = minResistance;
            if (maxWeight < minWeight) maxWeight = minWeight;
        }
    }
}