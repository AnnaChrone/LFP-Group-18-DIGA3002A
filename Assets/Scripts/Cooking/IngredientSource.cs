using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sushi.Data;

[RequireComponent(typeof(Image))]
public class IngredientSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Data")]
    public ItemData itemData;

    [Header("Drag Settings")]
    public GameObject dragPrefab;

    private GameObject currentDragObject;

    private void Start()
    {
        if (itemData != null && itemData.icon != null)
        {
            GetComponent<Image>().sprite = itemData.icon;
            GetComponent<Image>().color = itemData.tint;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData == null || dragPrefab == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        currentDragObject = Instantiate(dragPrefab, canvas.transform);

        Image ghostImage = currentDragObject.GetComponent<Image>();
        if (ghostImage != null)
        {
            ghostImage.sprite = itemData.icon;
            ghostImage.color = itemData.tint;
        }

        currentDragObject.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentDragObject != null)
        {
            currentDragObject.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Clean up the visual ghost. The slot itself handles storing the data.
        if (currentDragObject != null)
        {
            Destroy(currentDragObject);
            currentDragObject = null;
        }
    }
}