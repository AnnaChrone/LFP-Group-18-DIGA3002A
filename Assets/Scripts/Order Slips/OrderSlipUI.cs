using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Sushi.Data;
using Sushi.Inventory;

namespace Sushi.UI
{
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
        public Color acceptedTint = new Color(1f, 0.9f, 0.4f, 1f);   // yellow
        public Color servedTint = new Color(0.4f, 1f, 0.4f, 1f);     // green

        [Header("Plate Display")]
        [Tooltip("The Image on the physical plate that shows the sushi when ready, then the coin when served.")]
        public Image plateDisplay;

        [Tooltip("Sprite shown when the order is successfully served.")]
        public Sprite coinSprite;

        [Header("Served Animation")]
        [Tooltip("Seconds the green slip + coin stay fully visible before fading.")]
        public float holdAfterServeDuration = 1.5f;

        [Tooltip("Seconds the slip + coin take to fade out after the hold.")]
        public float fadeOutDuration = 0.3f;

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

        public bool IsAccepted { get; private set; }
        public RecipeData AssignedRecipe => assignedRecipe;

        private void Start()
        {
            if (interactPromptVisual != null) interactPromptVisual.SetActive(false);

            if (slipBackground != null)
                originalSlipColor = slipBackground.color;
        }

        public void InitializeRecipeTicket(RecipeData recipe, Inventory.Inventory inventory, OrderManager manager, int plateIndex)
        {
            assignedRecipe = recipe;
            liveInventoryReference = inventory;
            runtimeManager = manager;
            occupiedPlateIndex = plateIndex;
            IsAccepted = false;
            isServing = false;

            if (slipBackground != null)
                slipBackground.color = originalSlipColor;

            if (menuTitleText != null) menuTitleText.text = recipe.recipeName;

            if (menuIconDisplay != null && recipe.resultItem != null)
            {
                menuIconDisplay.sprite = recipe.resultItem.icon;
                menuIconDisplay.color = recipe.resultItem.tint;
            }

            if (goldPayoutText != null)
                goldPayoutText.text = $"+{recipe.recipeValue} Gold";

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
            // Only show the plate icon while the player is standing at the plate
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

        // --- Prompt / plate ---

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

        /// <summary>
        /// Shows the sushi icon on the plate ONLY when the player is in range
        /// and the dish is ready in inventory. Hides otherwise.
        /// </summary>
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

        // --- Accept ---

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

            RefreshInteractPrompt();
            RefreshPlateDisplay();
        }

        // --- Serve ---

        private void ServeOrder()
        {
            if (!IsAccepted || liveInventoryReference == null || assignedRecipe == null) return;
            if (isServing) return;

            ItemData platedSushi = assignedRecipe.resultItem;
            if (platedSushi == null || liveInventoryReference.CountOf(platedSushi) <= 0) return;

            isServing = true;

            liveInventoryReference.RemoveFirst(platedSushi);

            UiPrompter.Instance.CorrectOrder();
            Debug.Log($"Served: {assignedRecipe.recipeName}! Order complete.");

            if (runtimeManager != null)
            {
                runtimeManager.FreeUpPlateSlot(occupiedPlateIndex);
                runtimeManager.counter += assignedRecipe.recipeValue;
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

            // 6. Dismiss the ticket
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
            Debug.Log($"Order '{assignedRecipe.recipeName}' was rejected/cancelled by the player.");

            if (runtimeManager != null)
            {
                if (IsAccepted)
                    runtimeManager.CancelActiveOrder(gameObject);

                runtimeManager.FreeUpPlateSlot(occupiedPlateIndex);
                runtimeManager.DismissTicket(gameObject);
            }
        }

        // --- Trigger handling ---

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