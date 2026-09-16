using NUnit.Framework.Interfaces;
using Sushi.Data;

namespace Sushi.Inventory
{
    /// <summary>
    /// One physical catch sitting in a slot.
    ///
    /// The distinction from ItemData matters: ItemData is the shared asset
    /// describing what a tuna *is*, while CaughtItem is the particular tuna
    /// the player just landed. Milestone 2 makes weight depend on how well
    /// the player fought the fish, so two tuna in the bag can weigh different
    /// amounts. Storing instances now means that milestone adds a number to
    /// this struct rather than restructuring the inventory.
    ///
    /// Prototype 1 ignores weight entirely and treats every catch as one slot.
    /// </summary>
    [System.Serializable]
    public struct CaughtItem
    {
        public ItemData data;

        [UnityEngine.Tooltip("Actual weight of this specific catch. Defaults to the item's base weight.")]
        public float weight;

        [UnityEngine.Tooltip("0 to 1 rating of how cleanly the fish was landed. Milestone 2 prices on this.")]
        public float quality;

        public CaughtItem(ItemData data)
        {
            this.data = data;
            this.weight = data != null ? data.weight : 0f;
            this.quality = 0.5f;
        }

        public CaughtItem(ItemData data, float weight, float quality)
        {
            this.data = data;
            this.weight = weight;
            this.quality = UnityEngine.Mathf.Clamp01(quality);
        }

        public bool IsValid => data != null;
        public string Label => data != null ? data.Label : "—";

        public static readonly CaughtItem None = new CaughtItem(null, 0f, 0f);
    }
}