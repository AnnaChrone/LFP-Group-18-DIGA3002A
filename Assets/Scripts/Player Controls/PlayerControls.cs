using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private int facingDirection = 1;
    [SerializeField] public bool GamePaused = false;

    [Header("Fishing Variables")]
    public FishingManager fishingManager;
    public Cooking cooking;
    public ServeStation serveStation;

    [Header("Pause UI")]
    public PauseMenu pauseFunctions;
    public GameObject BaitShopPanel;

    [SerializeField] private SpriteRenderer playrSprite;


    private bool inStoveRange = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (GamePaused || fishingManager.busyFishing || BaitShopPanel.activeInHierarchy)
        {
            rb.linearVelocity = Vector2.zero;
            SoundManager.Instance.SetWalking(false);
            return;
        }

        rb.linearVelocity = moveInput * moveSpeed;
        SoundManager.Instance.SetWalking(moveInput != Vector2.zero);
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        if (moveInput.x != 0)
            playrSprite.flipX = moveInput.x > 0;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (GamePaused) return;
        if (!context.started) return;

        // Fishing takes priority if the player is in a fishing zone
        if (fishingManager.inFishingRange)
        {
            fishingManager.InteractPressed();
            return;
        }

        // Otherwise, toggle the stove if in range
        if (inStoveRange)
        {
            // If the stove UI is open, close it; otherwise open it.
            if (cooking.stoveUIPanel != null && cooking.stoveUIPanel.activeInHierarchy)
            {
                cooking.CloseStoveUI();
            }
            else
            {
                cooking.OpenStoveUI();
            }
        }
    }

    public void OnFish(InputAction.CallbackContext context)
    {
        if (GamePaused) return;
        if (!fishingManager.inFishingRange) return;

        if (context.started)
        {
            fishingManager.FishPressed();
        }

        if (context.canceled)
        {
            fishingManager.FishReleased();
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        GamePaused = !GamePaused;
        if (GamePaused)
        {
            rb.linearVelocity = Vector2.zero;
            SoundManager.Instance.SetWalking(false);
        }
        pauseFunctions.togglePauseMenu(GamePaused);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Stove"))
        {
            inStoveRange = true;
            Debug.Log("In stove range — press E to open");
            UiPrompter.Instance.StoveText(inStoveRange);
        }
        else if (other.CompareTag("Serve"))
        {
            serveStation.ServeButton();
            Debug.Log("Opening serve");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Stove"))
        {
            inStoveRange = false;
            cooking.CloseStoveUI();
            Debug.Log("Left stove range — closing UI");
            UiPrompter.Instance.StoveText(inStoveRange);
        }
        else if (other.CompareTag("Serve"))
        {
            serveStation.CloseServeButton();
            Debug.Log("closing serve");
        }
    }
}