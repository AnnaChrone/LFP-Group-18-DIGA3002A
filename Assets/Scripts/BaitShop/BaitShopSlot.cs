using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Sushi.Data;

public class BaitShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;

    private BaitData baitData;
    private BaitShopManager shopManager;

    public void SetupSlot(BaitData data, BaitShopManager manager)
    {
        baitData = data;
        shopManager = manager;

        iconImage.sprite = data.icon;
        nameText.text = data.displayName;
        priceText.text = data.price + "G";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(BuyBait);
    }

    private void BuyBait()
    {
        shopManager.TryPurchaseBait(baitData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        shopManager.ShowTooltip(baitData, transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        shopManager.HideTooltip();
    }
}
