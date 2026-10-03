using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sushi.UI
{
    [Serializable]
    public class TabEntry
    {
        [Tooltip("The clickable tab, e.g. the net icon button for Inventory.")]
        public Button button;

        [Tooltip("The same button's Image, tinted lighter when this tab is active, dimmer when it isn't.")]
        public Image buttonBackground;

        [Tooltip("The content panel this tab reveals. Only one is active at a time.")]
        public GameObject content;

        [Tooltip("Tick for the Sushi tab. It stays hidden and unselectable while at the dock (GameState.Daytime).")]
        public bool restaurantOnly;
    }

    /// <summary>
    /// The browser-style tab bar wrapping Inventory / Bait / Sushi.
    ///
    /// DayNightCycleManager has no change event, currentState is a plain
    /// public field set mid-coroutine, so this polls it once a frame rather
    /// than subscribing to anything. The check itself is cheap (one enum
    /// comparison), and tab visibility is only actually reapplied when the
    /// state has changed since the last check.
    /// </summary>
    public class TabGroup : MonoBehaviour
    {
        [SerializeField] private List<TabEntry> tabs = new List<TabEntry>();

        [Tooltip("Index into Tabs to open on start. Ignored if that tab is restaurant-only and it isn't available yet.")]
        [SerializeField] private int defaultTabIndex = 0;

        [Header("Tab Look")]
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        private int activeIndex = -1;
        private GameState lastKnownState;
        private bool hasCheckedState;

        public event Action<int> OnTabChanged;

        private void Awake()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i; // capture for the closure
                if (tabs[i].button != null)
                    tabs[i].button.onClick.AddListener(() => SelectTab(index));
            }
        }

        private void Start()
        {
            ApplyTabAvailability();

            int startIndex = defaultTabIndex;
            if (startIndex < 0 || startIndex >= tabs.Count || !IsAvailable(tabs[startIndex]))
                startIndex = FirstAvailableIndex();

            if (startIndex >= 0) SelectTab(startIndex);
        }

        private void Update()
        {
            GameState current = CurrentState();

            if (!hasCheckedState || current != lastKnownState)
            {
                hasCheckedState = true;
                lastKnownState = current;

                ApplyTabAvailability();

                if (activeIndex < 0 || !IsAvailable(tabs[activeIndex]))
                {
                    int fallback = FirstAvailableIndex();
                    if (fallback >= 0) SelectTab(fallback);
                }
            }
        }

        private GameState CurrentState()
        {
            return DayNightCycleManager.Instance != null
                ? DayNightCycleManager.Instance.currentState
                : GameState.Daytime;
        }

        private void ApplyTabAvailability()
        {
            foreach (var tab in tabs)
            {
                bool available = !tab.restaurantOnly || IsAvailable(tab);
                if (tab.button != null) tab.button.gameObject.SetActive(available);
            }
        }

        // Restaurant-only tabs count as available once the player has left
        // the dock at all (Nighttime or actively serving), not only during
        // the timed RestaurantService window. Change this to
        // "lastKnownState == GameState.RestaurantService" if the Sushi tab
        // should stay hidden until the countdown actually starts.
        private bool IsAvailable(TabEntry tab)
        {
            if (!tab.restaurantOnly) return true;
            return CurrentState() != GameState.Daytime;
        }

        private int FirstAvailableIndex()
        {
            for (int i = 0; i < tabs.Count; i++)
                if (IsAvailable(tabs[i])) return i;
            return -1;
        }

        /// <summary>Opens a tab by index, closing whichever was open before.</summary>
        public void SelectTab(int index)
        {
            if (index < 0 || index >= tabs.Count) return;
            if (!IsAvailable(tabs[index])) return;

            for (int i = 0; i < tabs.Count; i++)
            {
                bool isThisOne = i == index;
                if (tabs[i].content != null) tabs[i].content.SetActive(isThisOne);
                if (tabs[i].buttonBackground != null)
                    tabs[i].buttonBackground.color = isThisOne ? activeColor : inactiveColor;
            }

            activeIndex = index;
            OnTabChanged?.Invoke(index);
        }
    }
}