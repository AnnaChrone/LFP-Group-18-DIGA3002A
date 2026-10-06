using System;
using UnityEngine;
using Sushi.Data;
using Sushi.Inventory;

namespace Sushi.Fishing
{
    /// <summary>
    /// The one and only bridge between Joanna's FishingManager and Darryn's
    /// inventory. FishingManager calls these methods on it:
    ///
    ///   CanStartFishing  — before a cast, to enforce the capacity constraint
    ///   RollHookedFish   — on a bite, to decide the species and its resistance
    ///   ResolveCatch     — on a successful landing, to score and store the fish
    ///   ClearHookedFish  — when the fish gets away
    ///
    /// FishingManager never references Inventory, ItemData or BaitData directly,
    /// so the two systems can keep being developed on separate branches without
    /// merge conflicts in either file.
    /// </summary>
    public class FishingCatchHandler : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Sushi.Inventory.Inventory inventory;
        private BaitData castBaitOverride;

        [Tooltip("Standard bait for Prototype 1. The bait shop swaps this at Milestone 2.")]
        [SerializeField] private BaitData equippedBait;

        [Header("Milestone 2 — Fish Quality")]
        [Tooltip("Off: every catch uses the item's flat weight. " +
                 "On: weight runs from the species' min to max based on how well the fight went.")]
        [SerializeField] private bool useProficiencyWeighting = true;

        [Header("Proficiency — Speed")]
        [Tooltip("A fight this short or shorter earns full speed marks (at resistance 0.5).")]
        [SerializeField, Min(0.5f)] private float fastFightSeconds = 6f;

        [Tooltip("A fight this long or longer earns zero speed marks (at resistance 0.5).")]
        [SerializeField, Min(1f)] private float slowFightSeconds = 25f;

        [Header("Proficiency — Tension Control")]
        [Tooltip("Seconds spent at the danger tension (90%+) that earn zero control marks.")]
        [SerializeField, Min(0.1f)] private float maxDangerSeconds = 4f;

        [Header("Proficiency — Blend")]
        [Tooltip("How much of the score comes from speed. The rest comes from tension control.")]
        [SerializeField, Range(0f, 1f)] private float speedWeight = 0.5f;

        [Tooltip("Proficiency at or above this counts as a perfect catch and gives the max weight.")]
        [SerializeField, Range(0.5f, 1f)] private float perfectProficiency = 0.9f;

        // The species rolled at the moment of the bite. Held here so the
        // manager never needs to know about ItemData.
        private ItemData hookedItem;

        // --- Events for UI and audio --------------------------------------
        /// <summary>A fish was landed and stored.</summary>
        public event Action<CaughtItem> OnCatchStored;
        /// <summary>A cast was refused. The string is a player-facing reason.</summary>
        public event Action<string> OnCastBlocked;

        public BaitData EquippedBait => equippedBait;

        private void Awake()
        {
            if (inventory == null) inventory = FindObjectOfType<Sushi.Inventory.Inventory>();
        }

        // --- Called by FishingManager ---------------------------------------

        /// <summary>
        /// Gate on casting. This is the whole pacing mechanic: capacity, not a
        /// timer, is what stops the player fishing (GDD section 2).
        ///
        /// The check happens here rather than after the fight so a fish is
        /// never lost to a full bag.
        /// </summary>
        /// 
        public void SetBaitForThisCast(BaitData bait)
        {
            castBaitOverride = bait;
        }
        public bool CanStartFishing(out string reason)
        {
            if (inventory == null)
            {
                reason = "No inventory assigned.";
                return false;
            }

            if (inventory.IsFull)
            {
                reason = "Bag is full — head back to the stand.";
                OnCastBlocked?.Invoke(reason);
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>Overload for call sites that do not want the reason string.</summary>
        public bool CanStartFishing() => CanStartFishing(out _);

        /// <summary>
        /// Called on bite. Rolls the equipped bait's catch table to decide which
        /// species is on the line, and returns that species' resistance range
        /// so the fight can be tuned to it.
        /// </summary>
        public bool RollHookedFish(out float minResistance, out float maxResistance)
        {
            // Prefer the bait that was actually on the hook this cast
            BaitData baitToUse = castBaitOverride != null ? castBaitOverride : equippedBait;

            hookedItem = baitToUse != null ? baitToUse.Roll() : null;

            if (hookedItem == null)
            {
                Debug.LogWarning("[FishingCatchHandler] Nothing rolled on bite; using fallback resistance.", this);
                minResistance = 0.2f;
                maxResistance = 1f;
                return false;
            }

            minResistance = hookedItem.minResistance;
            maxResistance = hookedItem.maxResistance;
            return true;
        }

        /// <summary>Called when the fish gets away (line broke, withdrew, forced stop).</summary>
        public void ClearHookedFish()
        {
            hookedItem = null;
            castBaitOverride = null; // next cast will set a fresh bait
        }

        /// <summary>
        /// Called the moment FishingManager lands a fish. Scores the fight and
        /// stores the species that was rolled on bite.
        /// </summary>
        /// <param name="fightDuration">Seconds from hook to landing.</param>
        /// <param name="timeAtHighTension">Seconds the rod spent at or above the danger threshold.</param>
        /// <param name="averageResistance">Fish resistance averaged over the fight, 0 to 1.</param>
        public CaughtItem ResolveCatch(float fightDuration, float timeAtHighTension, float averageResistance)
        {
            if (inventory == null)
            {
                Debug.LogWarning("[FishingCatchHandler] No inventory assigned; catch discarded.", this);
                hookedItem = null;
                castBaitOverride = null;
                return CaughtItem.None;
            }

            ItemData rolled = hookedItem;
            hookedItem = null;

            // Safety net: if we somehow get here without a bite roll, use the cast override
            if (rolled == null)
            {
                BaitData baitToUse = castBaitOverride != null ? castBaitOverride : equippedBait;
                if (baitToUse != null) rolled = baitToUse.Roll();
            }

            castBaitOverride = null; // Cast is consumed

            if (rolled == null)
            {
                Debug.LogWarning("[FishingCatchHandler] No fish to resolve (no bait, or bait table empty).", this);
                return CaughtItem.None;
            }

            CaughtItem result = BuildCatch(rolled, fightDuration, timeAtHighTension, averageResistance);

            if (inventory.TryAdd(result))
            {
                OnCatchStored?.Invoke(result);
                return result;
            }

            Debug.LogWarning($"[FishingCatchHandler] No room for {result.Label} after a successful landing.", this);
            return CaughtItem.None;
        }

        /// <summary>Simple overload where the fight is not scored.</summary>
        public CaughtItem ResolveCatch() => ResolveCatch(0f, 0f, 0.5f);

        // --- Scoring -----------------------------------------------------------

        private CaughtItem BuildCatch(ItemData item, float fightDuration, float timeAtHighTension, float averageResistance)
        {
            if (!useProficiencyWeighting)
            {
                Debug.Log($"[Catch] {item.Label} | proficiency weighting off | weight: {item.weight:F2}");
                return new CaughtItem(item);
            }

            // Tougher fish legitimately take longer, so their time window stretches.
            float difficultyScale = Mathf.Lerp(0.6f, 1.6f, Mathf.Clamp01(averageResistance));
            float fastTime = fastFightSeconds * difficultyScale;
            float slowTime = Mathf.Max(slowFightSeconds * difficultyScale, fastTime + 0.01f);

            // 1 = at or under fastTime, 0 = at or over slowTime.
            float speedScore = 1f - Mathf.InverseLerp(fastTime, slowTime, fightDuration);

            // 1 = never hit the danger zone, 0 = spent maxDangerSeconds or more there.
            float controlScore = 1f - Mathf.Clamp01(timeAtHighTension / maxDangerSeconds);

            float proficiency = Mathf.Clamp01((speedScore * speedWeight) + (controlScore * (1f - speedWeight)));

            // Remap so a near-perfect fight reaches the max weight without needing exactly 1.0.
            float weightT = Mathf.Clamp01(proficiency / perfectProficiency);

            float weight = item.WeightForProficiency(weightT);

            Debug.Log(
                $"[Catch] {item.Label} | proficiency: {proficiency:P0} " +
                $"(speed {speedScore:P0}, control {controlScore:P0}) | " +
                $"fight: {fightDuration:F1}s (fast {fastTime:F1}s, slow {slowTime:F1}s) | " +
                $"time at danger tension: {timeAtHighTension:F1}s | " +
                $"weight: {weight:F2} (range {item.minWeight:F2} to {item.maxWeight:F2})"
            );

            return new CaughtItem(item, weight, proficiency);
        }

        // --- Bait shop API ------------------------------------------------------

        public void EquipBait(BaitData bait)
        {
            if (bait != null) equippedBait = bait;
        }
    }
}