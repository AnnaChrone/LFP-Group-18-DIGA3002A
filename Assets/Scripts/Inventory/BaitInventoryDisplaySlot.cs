using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sushi.Data;
using Sushi.Inventory; // Uses your data/inventory structures

namespace Sushi.UI
{
    public class BaitInventoryDisplaySlot : MonoBehaviour
    {
        [Header("Bait Binding")]
        [Tooltip("Assign the specific BaitData asset this slot is permanently locked to.")]
        [SerializeField] private BaitData targetBaitData;

        [Header("UI Visual Bindings")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI stackCountText;

        private void OnEnable()
        {
            // Automatically subscribe to updates when this tab page is opened
            if (BaitInventory.Instance != null)
            {
                BaitInventory.Instance.OnBaitInventoryChanged += RefreshUI;
            }
            RefreshUI();
        }

        private void OnDisable()
        {
            // Unsubscribe when the tab page closes to avoid memory leaks
            if (BaitInventory.Instance != null)
            {
                BaitInventory.Instance.OnBaitInventoryChanged -= RefreshUI;
            }
        }

        public void RefreshUI()
        {
            if (targetBaitData == null || BaitInventory.Instance == null) return;

            // 1. Force the slot to always display this specific bait's icon
            if (iconImage != null)
            {
                iconImage.sprite = targetBaitData.icon;
                iconImage.enabled = targetBaitData.icon != null;
            }

            // 2. Fetch the current item quantity count from our custom storage dictionary
            int count = BaitInventory.Instance.GetBaitCount(targetBaitData);

            // 3. Update the UI text to show the numerical stack size
            if (stackCountText != null)
            {
                stackCountText.text = count.ToString();
                
                // Purely visual: Fade out the text slightly if the player has 0 
                stackCountText.color = count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            }
        }
    }
}
