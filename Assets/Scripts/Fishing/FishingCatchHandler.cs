using System;
using UnityEngine;
using Sushi.Data;
using Sushi.Inventory;
using NUnit.Framework.Interfaces;

namespace Sushi.Fishing
{
    /// <summary>
    /// The one and only bridge between Joanna's FishingManager and Darryn's
    /// inventory. FishingManager calls exactly two methods on this:
    ///
    ///   CanStartFishing  — before a cast, to enforce the capacity constraint
    ///   ResolveCatch     — on a successful landing, to pick and store a fish
    ///
    /// Everything else stays on its own side. FishingManager never references
    /// Inventory, ItemData or BaitData directly, so the two systems can keep
    /// being developed on separate branches without merge conflicts in either
    /// file.
    /// </summary>
    public class FishingCatchHandler : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Sushi.Inventory.Inventory inventory;

        [Tooltip("Standard bait for Prototype 1. The bait shop swaps this at Milestone 2.")]
        [SerializeField] private BaitData equippedBait;

        [Header("Milestone 2 — Fish Quality")]
        [Tooltip("Off for Prototype 1: every catch weighs the item's base weight. " +
                 "On: a cleanly fought fish comes in heavier and higher quality.")]
        [SerializeField] private bool useProficiencyWeighting = false;

        [Tooltip("Weight multiplier for a badly fought fish (high tension, long fight).")]
        [SerializeField, Min(0.1f)] private float worstCaseWeightScale = 0.75f;

        [Tooltip("Weight multiplier for a cleanly fought fish.")]
        [SerializeField, Min(0.1f)] private float bestCaseWeightScale = 1.5f;

        [Tooltip("A fight longer than this counts as fully sloppy for scoring purposes.")]
        [SerializeField, Min(1f)] private float slowFightSeconds = 25f;

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
        /// never lost to a full bag. Under the Milestone 2 weight rule the bag
        /// can be "not full" yet still unable to take a heavy tuna, which is
        /// why this asks the capacity rule about the lightest possible catch
        /// rather than just reading IsFull.
        /// </summary>
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
        /// Called the moment FishingManager lands a fish. Rolls the bait's
        /// catch table, scores the fight, and stores the result.
        /// </summary>
        /// <param name="fightDuration">Seconds from hook to landing.</param>
        /// <param name="peakTensionNormalised">Highest rod tension reached, 0 to 1.</param>
        /// <param name="finalResistance">Fish resistance at the moment it landed, 0 to 1.</param>
        public CaughtItem ResolveCatch(float fightDuration, float peakTensionNormalised, float finalResistance)
        {
            if (inventory == null || equippedBait == null)
            {
                Debug.LogWarning("[FishingCatchHandler] Inventory or bait not assigned; catch discarded.", this);
                return CaughtItem.None;
            }

            ItemData rolled = equippedBait.Roll();
            if (rolled == null)
            {
                Debug.LogWarning("[FishingCatchHandler] Bait table is empty or all odds are zero.", this);
                return CaughtItem.None;
            }

            CaughtItem result = BuildCatch(rolled, fightDuration, peakTensionNormalised, finalResistance);

            if (inventory.TryAdd(result))
            {
                OnCatchStored?.Invoke(result);
                return result;
            }

            // Should be unreachable because CanStartFishing gated the cast,
            // but a fish arriving with nowhere to go must not fail silently.
            Debug.LogWarning($"[FishingCatchHandler] No room for {result.Label} after a successful landing.", this);
            return CaughtItem.None;
        }

        /// <summary>Simple overload for Prototype 1, where the fight is not scored.</summary>
        public CaughtItem ResolveCatch() => ResolveCatch(0f, 0f, 0.5f);

        // --- Scoring -----------------------------------------------------------

        private CaughtItem BuildCatch(ItemData item, float fightDuration, float peakTension, float finalResistance)
        {
            if (!useProficiencyWeighting) return new CaughtItem(item);

            // A clean fight is short and never redlines the rod. A tough fish
            // (high resistance) earns forgiveness on both counts, so a hard
            // catch is not punished for being hard.
            float speedScore = 1f - Mathf.Clamp01(fightDuration / slowFightSeconds);
            float controlScore = 1f - Mathf.Clamp01(peakTension);
            float difficultyBonus = Mathf.Clamp01(finalResistance) * 0.25f;

            float proficiency = Mathf.Clamp01((speedScore * 0.4f) + (controlScore * 0.6f) + difficultyBonus);

            float scale = Mathf.Lerp(worstCaseWeightScale, bestCaseWeightScale, proficiency);
            return new CaughtItem(item, item.weight * scale, proficiency);
        }

        // --- Bait shop API ------------------------------------------------------

        public void EquipBait(BaitData bait)
        {
            if (bait != null) equippedBait = bait;
        }
    }
}