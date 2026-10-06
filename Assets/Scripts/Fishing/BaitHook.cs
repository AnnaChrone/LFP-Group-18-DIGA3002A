using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sushi.Data;
using Sushi.Inventory;

[RequireComponent(typeof(Image))]
public class BaitHook : MonoBehaviour, IDropHandler
{
    [Header("Current State")]
    [Tooltip("The bait currently on the hook. Null if empty.")]
    public BaitData attachedBait;

    [Header("Visuals")]
    [Tooltip("The Image that shows the bait. Assign this same GameObject's Image component.")]
    public Image baitIconDisplay;

    [Tooltip("The sprite shown when no bait is attached (your hand-drawn RodHook).")]
    public Sprite emptyHookSprite;

    private void Awake()
    {
        // Auto-assign to this GameObject's own Image if nothing was set
        if (baitIconDisplay == null)
            baitIconDisplay = GetComponent<Image>();

        // The hook Image must always stay enabled and raycastable
        // so it keeps receiving drops.
        baitIconDisplay.enabled = true;
        baitIconDisplay.raycastTarget = true;
    }

    private void Start()
    {
        // Cache the original sprite if the designer didn't assign one
        if (emptyHookSprite == null && baitIconDisplay != null)
            emptyHookSprite = baitIconDisplay.sprite;

        RefreshVisual();
    }

    public void OnDrop(PointerEventData eventData)
    {
        BaitSource source = eventData.pointerDrag?.GetComponent<BaitSource>();

        if (source == null || source.baitData == null)
        {
            Debug.LogWarning($"BaitHook: Dropped object '{eventData.pointerDrag?.name}' has no BaitSource or BaitData.");
            return;
        }

        // Infinite bait (ownedFromStart) bypasses the inventory check
        if (!source.baitData.ownedFromStart)
        {
            if (BaitInventory.Instance == null || BaitInventory.Instance.GetBaitCount(source.baitData) <= 0)
            {
                Debug.Log($"BaitHook: No {source.baitData.displayName} in inventory.");
                return;
            }
        }

        attachedBait = source.baitData;
        Debug.Log($"BaitHook: Attached {attachedBait.displayName}.");

        RefreshVisual();
    }

    public void ClearHook()
    {
        if (attachedBait == null) return;

        Debug.Log($"BaitHook: Removed {attachedBait.displayName}.");
        attachedBait = null;
        RefreshVisual();
    }

    private bool isConsuming = false;

    public BaitData ConsumeAndGetBaitForCast()
    {
        if (attachedBait == null) return null;

        BaitData castBait = attachedBait;
        isConsuming = true;

        if (!castBait.ownedFromStart)
        {
            if (BaitInventory.Instance == null ||
                !BaitInventory.Instance.TryConsumeBait(castBait, 1))
            {
                isConsuming = false;
                Debug.Log($"BaitHook: Couldn't consume {castBait.displayName} — out of stock.");
                ClearHook();
                return null;
            }
        }

        isConsuming = false;
        attachedBait = null;
        RefreshVisual();

        return castBait;
    }

    /// <summary>
    /// Swaps the sprite on the hook's single Image between the empty hook
    /// drawing and the attached bait icon. NEVER disables the Image —
    /// that would break the raycast target.
    /// </summary>
    private void RefreshVisual()
    {
        if (baitIconDisplay == null) return;

        if (attachedBait != null && attachedBait.icon != null)
        {
            baitIconDisplay.sprite = attachedBait.icon;
            baitIconDisplay.color = Color.white;
        }
        else
        {
            baitIconDisplay.sprite = emptyHookSprite;
            baitIconDisplay.color = Color.white;
        }

        // Ensure it stays enabled and clickable no matter what
        baitIconDisplay.enabled = true;
    }

    // Optional: auto-clear if inventory runs out
    private void OnEnable()
    {
        if (BaitInventory.Instance != null)
            BaitInventory.Instance.OnBaitInventoryChanged += CheckBaitStillAvailable;
    }

    private void OnDisable()
    {
        if (BaitInventory.Instance != null)
            BaitInventory.Instance.OnBaitInventoryChanged -= CheckBaitStillAvailable;
    }

    private void CheckBaitStillAvailable()
    {
        if (isConsuming) return; // don't wipe the hook while a cast is resolving
        if (attachedBait == null) return;
        if (attachedBait.ownedFromStart) return;

        if (BaitInventory.Instance != null && BaitInventory.Instance.GetBaitCount(attachedBait) <= 0)
        {
            Debug.Log($"BaitHook: {attachedBait.displayName} ran out — clearing hook.");
            ClearHook();
        }
    }
}