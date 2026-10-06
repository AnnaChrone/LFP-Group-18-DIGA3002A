using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sushi.Data;
using Sushi.Inventory;

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
            if (BaitInventory.Instance != null)
            {
                BaitInventory.Instance.OnBaitInventoryChanged += RefreshUI;
            }
            RefreshUI();
        }

        private void OnDisable()
        {
            if (BaitInventory.Instance != null)
            {
                BaitInventory.Instance.OnBaitInventoryChanged -= RefreshUI;
            }
        }

        public void RefreshUI()
        {
            if (targetBaitData == null || BaitInventory.Instance == null) return;

            if (iconImage != null)
            {
                iconImage.sprite = targetBaitData.icon;
                iconImage.enabled = targetBaitData.icon != null;
            }

            // Fetch the current item quantity count from our custom storage dictionary
            int count = BaitInventory.Instance.GetBaitCount(targetBaitData);

            // Update the UI text to show the numerical stack size
            if (stackCountText != null)
            {
                stackCountText.text = count.ToString();
                
                // Fade out the text slightly if the player has 0 
                stackCountText.color = count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            }
        }
    }
}
