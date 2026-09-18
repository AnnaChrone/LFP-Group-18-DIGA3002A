using UnityEngine;
using System.Collections.Generic;
using Sushi.UI;

// Delivery/serve station. Player carries a plated sushi here and hits Serve.
// This checks every currently-accepted order to see if the player is holding
// the matching plated dish (see OrderSlipUI.TryServeOrder / RecipeData.resultItem);
// the first one that matches gets served and dismissed.
public class ServeStation : MonoBehaviour
{
    [Header("References")]
    public OrderManager orderManager;
    public GameObject serveButton;

    /// <summary>Hook this up to your Serve button's OnClick().</summary>
    /// 

    public void ServeButton()
    {
        serveButton.SetActive(true);
    }

    public void CloseServeButton()
    {
        serveButton.SetActive(false);
    }
    public void ClickServeButton()
    {
        if (orderManager == null)
        {
            Debug.LogError("ServeStation: OrderManager reference is missing!");
            return;
        }

        // Snapshot the list first — TryServeOrder() can remove a ticket via
        // CompleteOrder(), which would break a live foreach over the original list.
        List<GameObject> ticketsToCheck = new List<GameObject>(orderManager.AcceptedTickets);

        foreach (GameObject ticketObj in ticketsToCheck)
        {
            if (ticketObj == null) continue;

            OrderSlipUI slip = ticketObj.GetComponent<OrderSlipUI>();
            if (slip == null) continue;

            if (slip.TryServeOrder())
            {
                // One serve action delivers one dish. Stop after the first match.
                return;
            }

            // Correct dish wasn't in inventory — check if the player served a WRONG
            // plated dish instead, which fails this order and frees up the board.
            if (slip.TryFailServeWithWrongDish(orderManager.globalRecipeBook))
            {
                return;
            }
        }

        Debug.LogWarning("Nothing in your bag matches an accepted order yet.");
    }
}