using UnityEngine;

public class RestaurantTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            DayNightCycleManager.Instance.PromptGoToRestaurant();
        }
    }
}
