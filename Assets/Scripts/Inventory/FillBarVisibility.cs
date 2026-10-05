using UnityEngine;

namespace Sushi.UI
{
    /// <summary>
    /// Hides the fill bar once the player leaves the dock.
    /// Uses a CanvasGroup instead of SetActive so this script keeps running
    /// and can bring the bar back on the next day.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FillBarVisibility : MonoBehaviour
    {
        [Tooltip("Ticked: hidden during Nighttime and RestaurantService. " +
                 "Unticked: hidden only during RestaurantService.")]
        [SerializeField] private bool hideDuringNight = true;

        private CanvasGroup group;
        private GameState lastState;
        private bool hasChecked;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            GameState current = DayNightCycleManager.Instance != null
                ? DayNightCycleManager.Instance.currentState
                : GameState.Daytime;

            if (hasChecked && current == lastState) return;
            hasChecked = true;
            lastState = current;

            bool visible = hideDuringNight
                ? current == GameState.Daytime
                : current != GameState.RestaurantService;

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}