using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sushi.Data;

namespace Sushi.UI
{
    /// <summary>
    /// A single cell of the slot grid. Three sprites stacked: a frame, the
    /// item icon, and a selection outline that sits on top.
    ///
    /// Holds no game state. It is told what to show and it reports clicks
    /// upward by index.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
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
        [SerializeField] private Color hoverTint = new Color(1f, 1f, 1f, 0.85f);

        public int Index { get; private set; }
        public ItemData Item { get; private set; }
        public bool IsEmpty => Item == null;

        /// <summary>Raised on left click. InventoryUI subscribes.</summary>
        public event Action<int> OnClicked;
        /// <summary>Raised on right click, used as the quick-drop gesture.</summary>
        public event Action<int> OnRightClicked;
        /// <summary>Raised on hover with the hovered item, or null when leaving.</summary>
        public event Action<ItemData> OnHovered;

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
                Sprite target = (item != null && filledFrame != null) ? filledFrame : emptyFrame;
                if (target != null) frameImage.sprite = target;
                frameImage.color = normalTint;
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionImage != null) selectionImage.enabled = selected;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right) OnRightClicked?.Invoke(Index);
            else OnClicked?.Invoke(Index);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (frameImage != null) frameImage.color = hoverTint;
            OnHovered?.Invoke(Item);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (frameImage != null) frameImage.color = normalTint;
            OnHovered?.Invoke(null);
        }
    }
}