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

    [Header("Availability")]
    [Tooltip("Opacity when the player has none of this bait.")]
    [Range(0f, 1f)] public float unavailableAlpha = 0.3f;

    private GameObject currentDragObject;
    private Image sourceImage;

    private void Awake()
    {
        sourceImage = GetComponent<Image>();
    }

    private void Start()
    {
        if (baitData != null && baitData.icon != null)
        {
            sourceImage.sprite = baitData.icon;
        }

        UpdateBaitCount();
    }

    private void FixedUpdate()
    {
        UpdateBaitCount();
    }

    public void UpdateBaitCount()
    {
        if (baitData == null || BaitInventory.Instance == null) return;

        int count = BaitInventory.Instance.GetBaitCount(baitData);

        // Infinite bait (ownedFromStart) is always treated as available
        bool available = baitData.ownedFromStart || count > 0;

        // Update the count text
        if (Baitcount != null)
        {
            // Show a dash for infinite bait instead of the fake int.MaxValue
            Baitcount.text = baitData.ownedFromStart ? "-" : count.ToString();
            Baitcount.color = available ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }

        // Grey out the source image when unavailable
        if (sourceImage != null)
        {
            Color c = sourceImage.color;
            c.a = available ? 1f : unavailableAlpha;
            sourceImage.color = c;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (baitData == null || dragPrefab == null) return;

        // Block dragging if the player has none (unless it's infinite bait)
        if (!baitData.ownedFromStart)
        {
            if (BaitInventory.Instance == null || BaitInventory.Instance.GetBaitCount(baitData) <= 0)
            {
                return;
            }
        }

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