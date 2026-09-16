using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sushi.UI
{
    /// <summary>
    /// The inventory fill indicator called for in GDD section 8: a persistent
    /// readout telling the player how close they are to being pushed into the
    /// night phase.
    ///
    /// It reads FillFraction and FillLabel off the inventory's capacity rule,
    /// so it keeps working unchanged when the slot cap becomes a weight cap.
    /// </summary>
    public class InventoryFillBar : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Sushi.Inventory.Inventory inventory;

        [Tooltip("An Image with Image Type set to Filled, Fill Method Horizontal.")]
        [SerializeField] private Image fillImage;

        [SerializeField] private TMP_Text label;

        [Header("Colour Ramp")]
        [SerializeField] private Color lowColor = new Color(0.35f, 0.72f, 0.85f);
        [SerializeField] private Color highColor = new Color(0.95f, 0.72f, 0.28f);
        [SerializeField] private Color fullColor = new Color(0.88f, 0.34f, 0.32f);

        [Tooltip("Fraction at which the bar starts warming toward the warning colour.")]
        [SerializeField, Range(0f, 1f)] private float warnAt = 0.6f;

        [Header("Motion")]
        [Tooltip("Seconds for the bar to catch up. Keep it short so the catch still feels instant.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        [Tooltip("Gentle pulse once the bag is full, to sell the pacing beat.")]
        [SerializeField] private bool pulseWhenFull = true;

        private float displayed;
        private float velocity;

        private void Awake()
        {
            if (inventory == null) inventory = FindObjectOfType<Sushi.Inventory.Inventory>();
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.OnChanged += Refresh;
            if (inventory != null) displayed = inventory.FillFraction;
            Refresh();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnChanged -= Refresh;
        }

        public void Refresh()
        {
            if (inventory == null || label == null) return;
            label.text = inventory.FillLabel;
        }

        private void Update()
        {
            if (inventory == null || fillImage == null) return;

            float target = inventory.FillFraction;
            displayed = smoothTime <= 0f
                ? target
                : Mathf.SmoothDamp(displayed, target, ref velocity, smoothTime);

            fillImage.fillAmount = displayed;

            Color color;
            if (inventory.IsFull)
            {
                color = fullColor;
                if (pulseWhenFull)
                {
                    float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f);
                    color *= pulse;
                    color.a = 1f;
                }
            }
            else
            {
                float t = warnAt >= 1f ? 0f : Mathf.InverseLerp(warnAt, 1f, displayed);
                color = Color.Lerp(lowColor, highColor, t);
            }

            fillImage.color = color;
        }
    }
}