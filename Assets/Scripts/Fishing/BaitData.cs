using System.Collections.Generic;
using UnityEngine;

namespace Sushi.Data
{
    [System.Serializable]
    public class CatchChance
    {
        public ItemData item;

        [Tooltip("Relative weight, not a percentage. An entry of 3 is three times as likely as an entry of 1.")]
        [Min(0f)] public float odds = 1f;
    }

    /// <summary>
    /// A bait type is nothing but a weighted list of possible catches.
    /// Standard bait spreads its odds evenly (random catch); corn and worms
    /// skew theirs toward prawn and salmon respectively.
    ///
    /// Because bait is data, adding the Milestone 2 bait shop means creating
    /// two more assets and a purchase check, not editing FishingController.
    /// </summary>
    [CreateAssetMenu(fileName = "Bait_New", menuName = "Sushi/Bait Data")]
    public class BaitData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Standard Bait";
        public Sprite icon;

        [Header("Shop (Milestone 2)")]
        [Tooltip("Coin cost. Standard bait is free and always owned.")]
        [Min(0)] public int price = 0;
        public bool ownedFromStart = true;

        [Header("Catch Table")]
        public List<CatchChance> table = new List<CatchChance>();

        /// <summary>
        /// Picks one item using the weighted table. Returns null only if the
        /// table is empty or every entry has zero odds, which the caller
        /// should treat as "no catch this time" rather than as an error.
        /// </summary>
        public ItemData Roll()
        {
            float total = 0f;
            for (int i = 0; i < table.Count; i++)
            {
                if (table[i] != null && table[i].item != null)
                    total += Mathf.Max(0f, table[i].odds);
            }

            if (total <= 0f) return null;

            float roll = Random.value * total;
            for (int i = 0; i < table.Count; i++)
            {
                var entry = table[i];
                if (entry == null || entry.item == null) continue;

                roll -= Mathf.Max(0f, entry.odds);
                if (roll <= 0f) return entry.item;
            }

            // Floating point fall-through: hand back the last valid entry.
            for (int i = table.Count - 1; i >= 0; i--)
            {
                if (table[i] != null && table[i].item != null) return table[i].item;
            }
            return null;
        }

        /// <summary>Readable percentage for a given item, for tooltips and playtest debugging.</summary>
        public float PercentFor(ItemData item)
        {
            float total = 0f, mine = 0f;
            foreach (var e in table)
            {
                if (e == null || e.item == null) continue;
                float w = Mathf.Max(0f, e.odds);
                total += w;
                if (e.item == item) mine += w;
            }
            return total <= 0f ? 0f : (mine / total) * 100f;
        }
    }
}