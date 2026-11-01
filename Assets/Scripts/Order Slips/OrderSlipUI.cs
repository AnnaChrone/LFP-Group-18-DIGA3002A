using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Sushi.Data;
using Sushi.Inventory;

namespace Sushi.UI
{
    [System.Serializable]
    public class PayoutPenalty
    {
        [Tooltip("Seconds after accepting when this band is reached.")]
        public float atSeconds = 10f;

        [Tooltip("Extra percent of the ORIGINAL payout removed when this band is reached. Adds to the bands before it.")]
        [Min(0f)] public float percentOff = 3f;
    }

    [DisallowMultipleComponent]
    public class OrderSlipUI : MonoBehaviour
    {
        [Header("Layout Assignments")]
        public TMP_Text menuTitleText;
        public Image menuIconDisplay;
        public TMP_Text goldPayoutText;

        [Tooltip("Kept for legacy prefab compatibility. Not used anymore.")]
        public TMP_Text ingredientsListText;

        [Header("Status")]
        public TMP_Text statusText;

        [Header("Acceptance Visual")]
        public Image slipBackground;

        [Header("Acceptance Colors")]
        public Color acceptedTint = new Color(1f, 0.9f, 0.4f, 1f);
        public Color servedTint = new Color(0.4f, 1f, 0.4f, 1f);

        [Header("Plate Display")]
        [Tooltip("The Image on the physical plate that shows the sushi when ready, then the coin when served.")]
        public Image plateDisplay;

        [Tooltip("Sprite shown when the order is successfully served.")]
        public Sprite coinSprite;

        [Header("Served Animation")]
        [Tooltip("Seconds the green slip + coin stay fully visible before fading.")]
        public float holdAfterServeDuration = 0.5f;

        [Tooltip("Seconds the slip + coin take to fade out after the hold.")]
        public float fadeOutDuration = 0.1f;

        [Header("Order Timer (counts up after accept)")]
        [Tooltip("Optional. Shows elapsed time on the slip, e.g. 0:42")]
        public TMP_Text timerText;

        [Tooltip("The most that can ever be taken off, as a percent of the original payout, however many bands are reached.")]
        [Range(0f, 100f)] public float maxPercentOff = 50f;

        [Tooltip("Each band removes an extra bit once its time is reached. They add up (up to the max above).")]
        public System.Collections.Generic.List<PayoutPenalty> penalties =
            new System.Collections.Generic.List<PayoutPenalty>
            {
                new PayoutPenalty { atSeconds = 10f, percentOff = 3f },
                new PayoutPenalty { atSeconds = 20f, percentOff = 5f },
                new PayoutPenalty { atSeconds = 30f, percentOff = 10f }
            };

        [Header("Counter Interactivity Overlay")]
        [SerializeField] private GameObject interactPromptVisual;
        [SerializeField] private TMP_Text interactPromptText;

        private RecipeData assignedRecipe;
        private Inventory.Inventory liveInventoryReference;
        private OrderManager runtimeManager;

        private int occupiedPlateIndex = -1;
        private bool playerInRange = false;
        private bool isServing = false;

        private Color originalSlipColor = Color.white;

        private BudgetTier customerType;
        private int dynamicGoldPayout;      // original payout for this order
        private int currentGoldPayout;      // payout after time penalties

        private Coroutine orderTimerRoutine;
        private float elapsed;

        public bool IsAccepted { get; private set; }
        public RecipeData AssignedRecipe => assignedRecipe;

        private void Start()
        {
            if (interactPromptVisual != null) interactPromptVisual.SetActive(false);

            if (slipBackground != null)
                originalSlipColor = slipBackground.color;
        }

        public void InitializeRecipeTicket(RecipeData recipe, Inventory.Inventory inventory, OrderManager manager, int plateIndex, BudgetTier customerTier)
        {
            assignedRecipe = recipe;
            liveInventoryReference = inventory;
            runtimeManager = manager;
            occupiedPlateIndex = plateIndex;
            customerType = customerTier;
            IsAccepted = false;
            isServing = false;

            if (slipBackground != null)
                slipBackground.color = originalSlipColor;

            float multiplier = 1.0f;
            string tierPrefix = "";

            switch (customerType)
            {
                case BudgetTier.Budget:
                    multiplier = 0.75f;
                    tierPrefix = " [Budget]";
                    break;
                case BudgetTier.Standard:
                    multiplier = 1.0f;
                    break;
                case BudgetTier.VIP:
                    multiplier = 1.5f;
                    tierPrefix = " [VIP]";
                    break;
            }

            dynamicGoldPayout = Mathf.RoundToInt(recipe.recipeValue * multiplier);

            if (menuTitleText != null)
                menuTitleText.text = $"{recipe.recipeName}\n{tierPrefix}";

            if (menuIconDisplay != null && recipe.resultItem != null)
            {
                menuIconDisplay.sprite = recipe.resultItem.icon;
                menuIconDisplay.color = recipe.resultItem.tint;
            }

            StopOrderTimer();
            currentGoldPayout = dynamicGoldPayout;
            RefreshPayoutText();
            if (timerText != null) timerText.text = "";

            if (ingredientsListText != null)
                ingredientsListText.text = "";

            if (statusText != null) statusText.text = "Press E to Accept";

            if (plateDisplay != null)
            {
                plateDisplay.sprite = null;
                plateDisplay.enabled = false;
            }

            RefreshInteractPrompt();
        }

        private void Update()
        {
            if (IsAccepted && !isServing)
            {
                RefreshPlateDisplay();
                if (playerInRange) RefreshInteractPrompt();
            }

            if (!playerInRange) return;
            if (Keyboard.current == null) return;
            if (!Keyboard.current.eKey.wasPressedThisFrame) return;

            if (!IsAccepted)
            {
                ClickAcceptOrderButton();
            }
            else if (HasDishReady())
            {
                ServeOrder();
            }
        }

        // --- Order timer (counts up, payout drops as bands are reached) ---

        private System.Collections.IEnumerator OrderTimerRoutine()
        {
            elapsed = 0f;
            int lastSecond = -1;

            while (true)
            {
                elapsed += Time.deltaTime;   // scaled time, so your pause button stops it

                int sec = (int)elapsed;
                if (sec != lastSecond && timerText != null)
                {
                    lastSecond = sec;
                    timerText.text = $"{sec / 60}:{sec % 60:00}";
                }

                float off = 0f;
                for (int i = 0; i < penalties.Count; i++)
                    if (elapsed >= penalties[i].atSeconds)
                        off += penalties[i].percentOff;
                off = Mathf.Min(off, maxPercentOff);

                int deduction = Mathf.CeilToInt(dynamicGoldPayout * off / 100f - 0.0001f);
                int newPayout = Mathf.Max(0, dynamicGoldPayout - deduction);
                if (newPayout != currentGoldPayout)
                {
                    currentGoldPayout = newPayout;
                    RefreshPayoutText();
                }

                yield return null;
            }
        }

        private void StopOrderTimer()
        {
            if (orderTimerRoutine != null)
            {
                StopCoroutine(orderTimerRoutine);
                orderTimerRoutine = null;
            }
        }

        private void RefreshPayoutText()
        {
            if (goldPayoutText == null) return;

            goldPayoutText.text = currentGoldPayout == dynamicGoldPayout
                ? $"{dynamicGoldPayout} Gold"
                : $"<s>{dynamicGoldPayout}</s> {currentGoldPayout} Gold";
        }

        private void RefreshInteractPrompt()
        {
            if (interactPromptText == null) return;

            if (!IsAccepted)
            {
                interactPromptText.text = "[E] to accept order";
                return;
            }

            interactPromptText.text = HasDishReady() ? "[E] to serve" : "";
        }

        private bool HasDishReady()
        {
            if (assignedRecipe == null || assignedRecipe.resultItem == null) return false;
            if (liveInventoryReference == null) return false;
            return liveInventoryReference.CountOf(assignedRecipe.resultItem) > 0;
        }

        private void RefreshPlateDisplay()
        {
            if (plateDisplay == null) return;

            if (assignedRecipe == null || assignedRecipe.resultItem == null)
            {
                plateDisplay.enabled = false;
                return;
            }

            bool ready = IsAccepted && HasDishReady() && playerInRange;
            plateDisplay.enabled = ready;

            if (ready)
            {
                plateDisplay.sprite = assignedRecipe.resultItem.icon;
                plateDisplay.color = assignedRecipe.resultItem.tint;
            }
        }

        public void ClickAcceptOrderButton()
        {
            if (assignedRecipe == null || IsAccepted) return;

            if (runtimeManager == null || !runtimeManager.AcceptOrder(assignedRecipe, gameObject))
            {
                if (statusText != null) statusText.text = "Order In Progress...";
                return;
            }

            IsAccepted = true;

            if (statusText != null) statusText.text = "Order Accepted";

            if (slipBackground != null)
                slipBackground.color = acceptedTint;

            StopOrderTimer();
            orderTimerRoutine = StartCoroutine(OrderTimerRoutine());

            RefreshInteractPrompt();
            RefreshPlateDisplay();
        }

        private void ServeOrder()
        {
            if (!IsAccepted || liveInventoryReference == null || assignedRecipe == null) return;
            if (isServing) return;

            ItemData platedSushi = assignedRecipe.resultItem;
            if (platedSushi == null || liveInventoryReference.CountOf(platedSushi) <= 0) return;

            isServing = true;
            StopOrderTimer();

            liveInventoryReference.RemoveFirst(platedSushi);

            UiPrompter.Instance.CorrectOrder();
            Debug.Log($"Served: {assignedRecipe.recipeName}! Order complete.");

            if (runtimeManager != null)
            {
                runtimeManager.FreeUpPlateSlot(occupiedPlateIndex);

                runtimeManager.counter += currentGoldPayout;
                runtimeManager.coinCounter.text = runtimeManager.counter.ToString();
            }

            StartCoroutine(ServeAnimationRoutine());
        }

        private System.Collections.IEnumerator ServeAnimationRoutine()
        {
            // 1. Slip turns green
            if (slipBackground != null)
                slipBackground.color = servedTint;

            // 2. Plate shows the coin at full opacity
            if (plateDisplay != null && coinSprite != null)
            {
                plateDisplay.enabled = true;
                plateDisplay.sprite = coinSprite;
                plateDisplay.color = Color.white;
            }

            // 3. Hide prompt and mark status
            if (interactPromptVisual != null)
                interactPromptVisual.SetActive(false);
            if (statusText != null)
                statusText.text = "Served!";

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayCoin();

            // 4. Hold at full opacity so the player sees the completed state
            yield return new WaitForSeconds(holdAfterServeDuration);

            // 5. Fast fade out
            float t = 0f;
            Color slipStart = slipBackground != null ? slipBackground.color : Color.white;
            Color plateStart = plateDisplay != null ? plateDisplay.color : Color.white;

            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(1f, 0f, t / fadeOutDuration);

                if (slipBackground != null)
                    slipBackground.color = new Color(slipStart.r, slipStart.g, slipStart.b, a);

                if (plateDisplay != null)
                    plateDisplay.color = new Color(plateStart.r, plateStart.g, plateStart.b, a);

                yield return null;
            }

            // 6. Dismiss the ticket visual element cleanly from execution arrays
            if (runtimeManager != null)
                runtimeManager.CompleteOrder(gameObject);
        }

        // --- Failure / reject ---

        public bool TryFailServeWithWrongDish(System.Collections.Generic.List<RecipeData> allRecipes)
        {
            if (!IsAccepted || liveInventoryReference == null || allRecipes == null) return false;

            foreach (RecipeData otherRecipe in allRecipes)
            {
                if (otherRecipe == null || otherRecipe == assignedRecipe || otherRecipe.resultItem == null) continue;

                if (liveInventoryReference.CountOf(otherRecipe.resultItem) > 0)
                {
                    StopOrderTimer();
                    liveInventoryReference.RemoveFirst(otherRecipe.resultItem);

                    Debug.Log($"Served the wrong dish ({otherRecipe.recipeName}) for order '{assignedRecipe.recipeName}' - order failed.");
                    UiPrompter.Instance.IncorrectOrder();
                    if (statusText != null) statusText.text = "Failed!";

                    if (slipBackground != null)
                        slipBackground.color = originalSlipColor;

                    if (runtimeManager != null)
                    {
                        runtimeManager.FreeUpPlateSlot(occupiedPlateIndex);
                        runtimeManager.FailOrder(gameObject);
                    }
                    return true;
                }
            }

            return false;
        }

        public void ClickRejectOrderButton()
        {
            StopOrderTimer();
            Debug.Log($"Order '{assignedRecipe.recipeName}' was rejected/cancelled by the player.");

            if (runtimeManager != null)
            {
                if (IsAccepted)
                    runtimeManager.CancelActiveOrder(gameObject);

                runtimeManager.FreeUpPlateSlot(occupiedPlateIndex);
                runtimeManager.DismissTicket(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                playerInRange = true;
                RefreshInteractPrompt();
                RefreshPlateDisplay(); // show sushi if it's ready
                if (interactPromptVisual != null) interactPromptVisual.SetActive(true);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                playerInRange = false;
                if (interactPromptVisual != null) interactPromptVisual.SetActive(false);

                // Hide plate sushi when the player walks away
                if (plateDisplay != null && !isServing)
                    plateDisplay.enabled = false;
            }
        }

        /// <summary>
        /// Public wrapper so external callers can still trigger a serve.
        /// Returns true if the dish was served.
        /// </summary>
        public bool TryServeOrder()
        {
            if (!IsAccepted || isServing) return false;
            if (!HasDishReady()) return false;

            ServeOrder();
            return true;
        }
    }
}