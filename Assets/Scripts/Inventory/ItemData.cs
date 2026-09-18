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
    /// Everything about an item lives in an asset, not in code, so new content
    /// can be added without touching the inventory or fishing systems.
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

        /// <summary>Safe display name even if the field was left blank in the asset.</summary>
        public string Label => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}