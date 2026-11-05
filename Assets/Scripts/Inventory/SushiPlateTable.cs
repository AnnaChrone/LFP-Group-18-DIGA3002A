using UnityEngine;
using UnityEngine.UI;
using Sushi.Data;

namespace Sushi.UI
{
    /// <summary>
    /// Sushi inventory as a panel with a fixed number of hand-placed plates
    /// (slot based, no weight). Each cooked sushi appears on the next free plate.
    ///
    /// The plates are free-standing sprites, no layout group needed. The layout
    /// you author in the scene is the COLLAPSED layout. While the cooking panel
    /// is open, the whole panel scales up uniformly by "Expand Multiplier", so the
    /// plates keep their positions and proportions.
    ///
    /// The cap itself lives in WeightCapacityRule.maxSushi: set it to the number
    /// of plates so the player can't cook more sushi than fits.
    /// </summary>
    public class SushiPlateTable : MonoBehaviour
    {
        [System.Serializable]
        public class PlateSlot
        {
            [Tooltip("The plate sprite (always visible as a placeholder).")]
            public Image plate;
            [Tooltip("Optional. A child Image on top of the plate that shows the sushi. " +
                     "If left empty, one is created automatically.")]
            public Image food;
        }

        [Header("Wiring")]
        [SerializeField] private Sushi.Inventory.Inventory inventory;

        [Tooltip("Drag Plate01..Plate04 here, in the order they should fill.")]
        [SerializeField] private PlateSlot[] plates = new PlateSlot[4];

        [Tooltip("Optional. Only used to warn you if its Max Sushi doesn't match the plate count.")]
        [SerializeField] private Sushi.Inventory.WeightCapacityRule capacityRule;

        [Header("Service Visibility")]
        [Tooltip("The object shown only during restaurant service (usually HotPlateSushiPanel). " +
                 "Put THIS script on a different, always-active object (the HUD canvas or a manager), " +
                 "because an inactive object can't run code.")]
        [SerializeField] private GameObject visualRoot;

        [Tooltip("Hide the Visual Root when the game starts.")]
        [SerializeField] private bool startHidden = true;

        [Header("Plate Look")]
        [Tooltip("How faded an empty plate looks (1 = fully visible).")]
        [SerializeField, Range(0f, 1f)] private float emptyPlateAlpha = 0.55f;

        [Tooltip("Gap between the plate edge and the sushi, used only for auto-created food images.")]
        [SerializeField, Min(0f)] private float autoFoodPadding = 6f;

        [Header("Expand While Cooking")]
        [Tooltip("The whole panel (HotPlateSushiPanel). It is scaled up uniformly while cooking, " +
                 "so the plates can't stretch. Nothing is resized, only scaled.")]
        [SerializeField] private RectTransform panel;

        [Tooltip("The cooking panel (Cooking.stoveUIPanel). The table expands whenever it is open.")]
        [SerializeField] private GameObject followPanel;

        [Tooltip("How much bigger the panel and plates get while cooking (1.5 = 50% larger).")]
        [SerializeField, Min(1f)] private float expandMultiplier = 1.5f;

        [SerializeField, Min(0.01f)] private float expandTime = 0.2f;

        [Tooltip("Optional extras (a title, a table decoration) shown only while expanded.")]
        [SerializeField] private GameObject[] showOnlyWhenExpanded;

        private Vector3 collapsedPanelScale = Vector3.one;
        private float amount; // 0 = collapsed, 1 = expanded

        private void Awake()
        {
            if (inventory == null) inventory = FindObjectOfType<Sushi.Inventory.Inventory>();

            if (visualRoot == gameObject)
                Debug.LogWarning("[SushiPlateTable] Visual Root is the same object as this script. " +
                                 "Move the script to an always-active object, or it can never be shown again.", this);

            if (visualRoot != null && startHidden) visualRoot.SetActive(false);

            // Whatever you authored in the scene is the collapsed state.
            if (panel != null) collapsedPanelScale = panel.localScale;

            if (plates == null || plates.Length == 0)
            {
                Debug.LogError("[SushiPlateTable] No plates assigned.", this);
                return;
            }

            for (int i = 0; i < plates.Length; i++)
            {
                PlateSlot slot = plates[i];
                if (slot == null || slot.plate == null)
                {
                    Debug.LogError($"[SushiPlateTable] Plate slot {i} has no plate Image assigned.", this);
                    continue;
                }

                if (slot.food == null) slot.food = CreateFoodChild(slot.plate);
            }
        }

        private void Start()
        {
            if (capacityRule != null && plates != null && capacityRule.maxSushi != plates.Length)
            {
                Debug.LogWarning($"[SushiPlateTable] Capacity rule allows {capacityRule.maxSushi} sushi but " +
                                 $"there are {plates.Length} plates. Set Max Sushi to {plates.Length}.", this);
            }
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.OnChanged += Refresh;
            Refresh();

            amount = IsCooking() ? 1f : 0f;
            ApplyExpansion();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.OnChanged -= Refresh;
        }

        private void Update()
        {
            float target = IsCooking() ? 1f : 0f;
            if (Mathf.Approximately(amount, target)) return;

            amount = Mathf.MoveTowards(amount, target, Time.unscaledDeltaTime / expandTime);
            ApplyExpansion();
        }

        private bool IsCooking()
        {
            return followPanel != null && followPanel.activeInHierarchy;
        }

        // Call when restaurant service begins.
        public void BeginService()
        {
            if (visualRoot != null) visualRoot.SetActive(true);
            Refresh();
        }

        // Call when restaurant service ends.
        public void EndService()
        {
            if (visualRoot != null) visualRoot.SetActive(false);
        }

        public void Refresh()
        {
            if (inventory == null || plates == null) return;

            int plate = 0;
            var items = inventory.Items;
            for (int i = 0; i < items.Count && plate < plates.Length; i++)
            {
                ItemData data = items[i].data;
                if (data == null || data.category != ItemCategory.Sushi) continue;

                ApplyFilled(plates[plate], data);
                plate++;
            }

            for (; plate < plates.Length; plate++)
                ApplyEmpty(plates[plate]);
        }

        private void ApplyExpansion()
        {
            float t = Mathf.SmoothStep(0f, 1f, amount);
            float scale = Mathf.Lerp(1f, expandMultiplier, t);

            if (panel != null)
                panel.localScale = collapsedPanelScale * scale;

            if (showOnlyWhenExpanded == null) return;
            bool extras = amount > 0f;
            for (int i = 0; i < showOnlyWhenExpanded.Length; i++)
                if (showOnlyWhenExpanded[i] != null) showOnlyWhenExpanded[i].SetActive(extras);
        }

        private Image CreateFoodChild(Image plate)
        {
            GameObject go = new GameObject("Food", typeof(RectTransform), typeof(Image));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(plate.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(autoFoodPadding, autoFoodPadding);
            rt.offsetMax = new Vector2(-autoFoodPadding, -autoFoodPadding);

            Image img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.enabled = false;
            return img;
        }

        private void ApplyFilled(PlateSlot slot, ItemData data)
        {
            if (slot == null) return;
            if (slot.plate != null) slot.plate.color = Color.white;

            if (slot.food != null)
            {
                bool hasIcon = data.icon != null;
                slot.food.enabled = hasIcon;
                if (hasIcon)
                {
                    slot.food.sprite = data.icon;
                    slot.food.color = data.tint;
                }
            }
        }

        private void ApplyEmpty(PlateSlot slot)
        {
            if (slot == null) return;
            if (slot.plate != null)
                slot.plate.color = new Color(1f, 1f, 1f, emptyPlateAlpha);

            if (slot.food != null) slot.food.enabled = false;
        }
    }
}