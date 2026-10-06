using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sushi.Data;

[RequireComponent(typeof(Image))]
public class SushiSlot : MonoBehaviour, IDropHandler
{
    [Header("Current State")]
    public ItemData currentItem;

    [Header("Manager Reference")]
    [Tooltip("The Cooking manager this slot reports to. Auto-finds if left empty.")]
    public Cooking cookingManager;

    private Image slotImage;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
    }

    private void Start()
    {
        if (cookingManager == null)
            cookingManager = FindObjectOfType<Cooking>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        IngredientSource source = eventData.pointerDrag?.GetComponent<IngredientSource>();

        if (source == null || source.itemData == null)
        {
            Debug.LogWarning($"Dropped object '{eventData.pointerDrag?.name}' has no IngredientSource or ItemData.");
            return;
        }

        currentItem = source.itemData;
        slotImage.sprite = currentItem.icon;
        slotImage.color = currentItem.tint;

        Debug.Log($"Slot {gameObject.name} now contains: {currentItem.displayName}");

        // Notify the manager to update the Make Sushi button
        if (cookingManager != null)
            cookingManager.RefreshMatchedRecipe();
    }

    public void ClearSlot()
    {
        currentItem = null;
        slotImage.sprite = null;
        slotImage.color = new Color(1, 1, 1, 0);

        if (cookingManager != null)
            cookingManager.RefreshMatchedRecipe();
    }
}