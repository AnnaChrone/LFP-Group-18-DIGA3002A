using UnityEngine;
using UnityEngine.UI;
using Sushi.Data;

namespace Sushi.UI
{
    /// <summary>
    /// A single visual cell of the inventory slot grid.
    /// The slot is display-only and does not accept player input.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("Sprite Layers")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image selectionImage;

        [Header("Frame States")]
        [SerializeField] private Sprite emptyFrame;
        [SerializeField] private Sprite filledFrame;

        [Header("Tints")]
        [SerializeField] private Color normalTint = Color.white;

        public int Index { get; private set; }
        public ItemData Item { get; private set; }
        public bool IsEmpty => Item == null;

        public void Initialise(int index)
        {
            Index = index;
            SetItem(null);
            SetSelected(false);
        }

        public void SetItem(ItemData item)
        {
            Item = item;

            if (iconImage != null)
            {
                bool hasItem = item != null && item.icon != null;
                iconImage.enabled = hasItem;

                if (hasItem)
                {
                    iconImage.sprite = item.icon;
                    iconImage.color = item.tint;
                }
            }

            if (frameImage != null)
            {
                Sprite target = (item != null && filledFrame != null)
                    ? filledFrame
                    : emptyFrame;

                if (target != null)
                    frameImage.sprite = target;

                frameImage.color = normalTint;
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionImage != null)
                selectionImage.enabled = selected;
        }
    }
}