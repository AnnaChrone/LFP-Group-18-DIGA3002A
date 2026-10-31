using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sushi.Data;

namespace Sushi.UI
{
    /// <summary>
    /// The inventory fill indicator called for in GDD section 8: a persistent
    /// readout telling the player how close they are to being pushed into the
    /// night phase.
    ///
    /// It reads FillFraction and FillLabel off the inventory's capacity rule,
    /// so it keeps working unchanged when the slot cap becomes a weight cap.
    ///
    /// The optional Weight Label counts the carried weight up (or down) in step
    /// with the bar, e.g. "6.4 / 10.0 kg".
    /// </summary>
    public class InventoryFillBar : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Sushi.Inventory.Inventory inventory;

        [Tooltip("An Image with Image Type set to Filled, Fill Method Horizontal.")]
        [SerializeField] private Image fillImage;

        [SerializeField] private TMP_Text label;

        [Header("Weight Readout")]
        [Tooltip("Live carried weight. Counts smoothly as the bar fills.")]
        [SerializeField] private TMP_Text weightLabel;

        [Tooltip("The weight rule asset (Capacity_Weight). Supplies the max weight and unit. " +
                 "Leave empty to show only the current weight.")]
        [SerializeField] private Sushi.Inventory.WeightCapacityRule weightRule;

        [SerializeField] private string weightFormat = "0.0";
        [SerializeField] private string fallbackUnit = "kg";

        [Tooltip("Tint the weight text with the bar colour.")]
        [SerializeField] private bool tintWeightLabel = true;

        [Header("Colour Ramp")]
        [SerializeField] private Color lowColor = new Color(0.35f, 0.72f, 0.85f);
        [SerializeField] private Color highColor = new Color(0.95f, 0.72f, 0.28f);
        [SerializeField] private Color fullColor = new Color(0.88f, 0.34f, 0.32f);

        [Tooltip("Fraction at which the bar starts warming toward the warning colour.")]
        [SerializeField, Range(0f, 1f)] private float warnAt = 0.6f;

        [Header("Motion")]
        [Tooltip("Seconds for the bar and weight number to catch up. Keep it short so the catch still feels instant.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        [Tooltip("Gentle pulse once the bag is full, to sell the pacing beat.")]
        [SerializeField] private bool pulseWhenFull = true;

        private float displayed;
        private float velocity;

        private float targetWeight;
        private float weightShown;
        private float weightVelocity;
        private float lastWrittenWeight = float.NaN;

        private void Awake()
        {
            if (inventory == null) inventory = FindObjectOfType<Sushi.Inventory.Inventory>();
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.OnChanged += Refresh;
            if (weightRule != null) weightRule.OnCapacityChanged += HandleCapacityChanged;   
            if (inventory != null)
            {
                displayed = inventory.FillFraction;
                targetWeight = CalculateFishWeight();
                weightShown = targetWeight;
            }
            lastWrittenWeight = float.NaN;
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnChanged -= Refresh;
            if (weightRule != null) weightRule.OnCapacityChanged -= HandleCapacityChanged;   

        }
        private void HandleCapacityChanged()
        {
            lastWrittenWeight = float.NaN;   // forces the weight text to rebuild with new max
            Refresh();
        }

        public void Refresh()
        {
            if (inventory == null) return;

            targetWeight = CalculateFishWeight();

            if (label != null) label.text = inventory.FillLabel;
        }

        private void Update()
        {
            if (inventory == null) return;

            float target = inventory.FillFraction;
            displayed = smoothTime <= 0f
                ? target
                : Mathf.SmoothDamp(displayed, target, ref velocity, smoothTime);

            weightShown = smoothTime <= 0f
                ? targetWeight
                : Mathf.SmoothDamp(weightShown, targetWeight, ref weightVelocity, smoothTime);

            Color color = CurrentColor();

            if (fillImage != null)
            {
                fillImage.fillAmount = displayed;
                fillImage.color = color;
            }

            if (weightLabel != null)
            {
                UpdateWeightText();
                if (tintWeightLabel) weightLabel.color = color;
            }
        }

        private Color CurrentColor()
        {
            if (inventory.IsFull)
            {
                Color c = fullColor;
                if (pulseWhenFull)
                {
                    float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f);
                    c *= pulse;
                    c.a = 1f;
                }
                return c;
            }

            float t = warnAt >= 1f ? 0f : Mathf.InverseLerp(warnAt, 1f, displayed);
            return Color.Lerp(lowColor, highColor, t);
        }

        private void UpdateWeightText()
        {
            // Only rebuild the string when the number visibly changes.
            if (!float.IsNaN(lastWrittenWeight) && Mathf.Abs(weightShown - lastWrittenWeight) < 0.005f)
                return;

            lastWrittenWeight = weightShown;

            string current = weightShown.ToString(weightFormat);
            weightLabel.text = weightRule != null
                ? $"{current} / {weightRule.maxWeight.ToString(weightFormat)} {weightRule.unitSuffix}"
                : $"{current} {fallbackUnit}";
        }

        // Fishing bag only: cooked sushi never counts toward the weight cap.
        private float CalculateFishWeight()
        {
            float w = 0f;
            var items = inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!item.IsValid) continue;
                if (item.data != null && item.data.category == ItemCategory.Sushi) continue;
                w += item.weight;
            }
            return w;
        }
    }
}