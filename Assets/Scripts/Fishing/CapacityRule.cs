using UnityEngine;
using Sushi.Data;

namespace Sushi.Inventory
{
    /// <summary>
    /// Decides what "full" means. This is the single seam between the
    /// Prototype 1 slot cap and the Milestone 2 weight cap.
    /// Nothing else in the project asks "how many slots are left" directly.
    /// The fishing controller, the fill bar and the day/night trigger all go
    /// through this, so replacing the rule asset on the Inventory component
    /// swaps the whole capacity model with no code changes.
    /// </summary>
    public abstract class CapacityRule : ScriptableObject
    {
        /// <summary>Can this specific item still be taken in?</summary>
        public abstract bool CanAccept(Inventory inv, CaughtItem item);

        /// <summary>True when the player should be pushed into the night phase.</summary>
        public abstract bool IsFull(Inventory inv);

        /// <summary>0 to 1, for the fill bar.</summary>
        public abstract float FillFraction(Inventory inv);

        /// <summary>Short human-readable state, e.g. "5 / 8" or "4.2 / 10.0 kg".</summary>
        public abstract string FillLabel(Inventory inv);
    }
}