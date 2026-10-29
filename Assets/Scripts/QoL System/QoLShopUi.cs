using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the QoL shop from QoLUpgradeDatabase.upgrades.
/// Add a new upgrade to the database list and it appears here automatically.
/// </summary>
public class QoLShopUI : MonoBehaviour
{
    [Header("References")]
    public Transform listContainer;      // e.g. the Content object of a Scroll View (with a Vertical Layout Group)
    public QoLShopItemUI itemPrefab;     // prefab with QoLShopItemUI on it

    readonly List<QoLShopItemUI> spawned = new List<QoLShopItemUI>();

    bool started;

    void Start()
    {
        started = true;
        Populate();
        Subscribe();
    }

    void OnEnable()
    {
        if (!started) return;   // first time is handled by Start
        Populate();
        Subscribe();
    }

    void OnDisable()
    {
        if (QoLManager.Instance != null)
            QoLManager.Instance.OnUpgradePurchased -= HandlePurchased;
    }

    void Subscribe()
    {
        if (QoLManager.Instance != null)
            QoLManager.Instance.OnUpgradePurchased += HandlePurchased;
    }

    public void Populate()
    {
        foreach (var item in spawned) if (item) Destroy(item.gameObject);
        spawned.Clear();

        var db = QoLManager.Instance;
        if (db == null) { Debug.LogWarning("No QoLUpgradeDatabase in scene."); return; }

        foreach (var upgrade in db.upgrades)
        {
            var item = Instantiate(itemPrefab, listContainer);
            item.Setup(upgrade, db);
            spawned.Add(item);
        }
    }

    void HandlePurchased(QoLUpgrade _) => RefreshAll();

    // Call this too whenever the player's coins change, so buttons grey out correctly.
    public void RefreshAll()
    {
        foreach (var item in spawned) item.Refresh();
    }
 
}

