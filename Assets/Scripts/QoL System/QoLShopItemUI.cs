using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Goes on the ROW prefab (QoLShopItem): one upgrade in the shop.
/// Prefab needs: name text, description text, price text, optional icon Image, and a Button.
/// </summary>
public class QoLShopItemUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text priceText;
    public Image iconImage;
    public Button buyButton;

    QoLUpgrade upgrade;
    QoLManager db;

    public void Setup(QoLUpgrade u, QoLManager manager)
    {
        upgrade = u;
        db = manager;

        nameText.text = u.displayName;
        descriptionText.text = u.description;
        if (iconImage) { iconImage.sprite = u.icon; iconImage.enabled = u.icon != null; }

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => db.TryPurchase(upgrade));

        Refresh();
    }

    public void Refresh()
    {
        if (upgrade.IsMaxed)
        {
            priceText.text = "Purchased";
            buyButton.interactable = false;
        }
        else
        {
            priceText.text = upgrade.CurrentPrice + " coins";
            buyButton.interactable = db.CanAfford(upgrade);
        }
    }
}