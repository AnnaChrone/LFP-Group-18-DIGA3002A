using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sushi.Data;

[RequireComponent(typeof(Image))]
public class BaitSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Data")]
    [Tooltip("The bait this source dispenses.")]
    public BaitData baitData;

    [Header("Drag Settings")]
    public GameObject dragPrefab;

    private GameObject currentDragObject;

    private void Start()
    {
        if (baitData != null && baitData.icon != null)
        {
            GetComponent<Image>().sprite = baitData.icon;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (baitData == null || dragPrefab == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        currentDragObject = Instantiate(dragPrefab, canvas.transform);

        Image ghostImage = currentDragObject.GetComponent<Image>();
        if (ghostImage != null)
        {
            ghostImage.sprite = baitData.icon;
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
        if (currentDragObject != null)
        {
            Destroy(currentDragObject);
            currentDragObject = null;
        }
    }
}