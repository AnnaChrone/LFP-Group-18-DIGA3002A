using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sushi.Data;

[RequireComponent(typeof(Image))]
public class SushiSlot : MonoBehaviour, IDropHandler
{
    [Header("Current State")]
    public ItemData currentItem;

    private Image slotImage;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        // The dragged object is the SOURCE (e.g. "Rice"), not the ghost.
        // So we read ItemData straight from its IngredientSource component.
        IngredientSource source = eventData.pointerDrag?.GetComponent<IngredientSource>();

        if (source == null || source.itemData == null)
        {
            Debug.LogWarning($"Dropped object '{eventData.pointerDrag?.name}' has no IngredientSource or ItemData.");
            return;
        }

        // Store the data
        currentItem = source.itemData;

        // Update the visual
        slotImage.sprite = currentItem.icon;
        slotImage.color = currentItem.tint;

        Debug.Log($"Slot {gameObject.name} now contains: {currentItem.displayName}");
    }

    public void ClearSlot()
    {
        currentItem = null;
        slotImage.sprite = null;
        slotImage.color = new Color(1, 1, 1, 0);
    }
}