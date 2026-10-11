using Sushi.Data;
using Sushi.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BaitSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Data")]
    [Tooltip("The bait this source dispenses.")]
    public BaitData baitData;

    [Header("Drag Settings")]
    public TextMeshProUGUI Baitcount;
    public GameObject dragPrefab;

    private GameObject currentDragObject;

    private void Start()
    {
        if (baitData != null && baitData.icon != null)
        {
            GetComponent<Image>().sprite = baitData.icon;
            UpdateBaitCount();
            
        }
    }

    private void FixedUpdate()
    {
        UpdateBaitCount();
    }

    public void UpdateBaitCount()
    {
        int count = BaitInventory.Instance.GetBaitCount(baitData);

        // Update the UI text to show the numerical stack size
        if (Baitcount != null)
        {
            Baitcount.text = count.ToString();

            // Fade out the text slightly if the player has 0 
            Baitcount.color = count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.4f);
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