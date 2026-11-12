using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
   

    [Header("Pause UI")]
    public PauseMenu pauseFunctions;
    public GameObject BaitShopPanel;
    public GameObject QoLShopPanel;
    public GameObject startMenu;

    [SerializeField] private SpriteRenderer playrSprite;


    private bool inStoveRange = false;
    private bool inQoLRange = false;
    private bool panelPauseActive = false;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (startMenu != null)
        {
            startMenu.SetActive(true);
        }
    }
    private bool AnyPanelOpen()
    {
        bool stove = cooking != null && cooking.stoveUIPanel != null && cooking.stoveUIPanel.activeInHierarchy;
        bool qol = QoLShopPanel != null && QoLShopPanel.activeInHierarchy;
        bool bait = BaitShopPanel != null && BaitShopPanel.activeInHierarchy;
        return stove || qol || bait;
    }

    private void Update()
    {
        bool open = AnyPanelOpen();
        if (open == panelPauseActive) return;

        panelPauseActive = open;

        if (open)
            Time.timeScale = 0f;
        else if (!GamePaused)          // don't un-freeze if the pause menu is up
            Time.timeScale = 1f;
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
        } else
        if (inQoLRange)
        {
            // If the QoL UI is open, close it; otherwise open it.
            if (QoLShopPanel.activeInHierarchy)
            {
                QoLShopPanel.SetActive(false);
            }
            else
            {
                QoLShopPanel.SetActive(true);
                
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
        if (!GamePaused && panelPauseActive) Time.timeScale = 0f;

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Stove"))
        {
            inStoveRange = true;
            Debug.Log("In stove range — press E to open");
            UiPrompter.Instance.StoveText(inStoveRange);
        }
        else if (other.CompareTag("QoLStation"))
        {
            inQoLRange = true;
            Debug.Log("In QoL range — press E to open");
            UiPrompter.Instance.QoLText(inQoLRange);
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
        else if (other.CompareTag("QoLStation"))
        {
            inQoLRange = false;
            Debug.Log("Left QoL range — closing UI");
            UiPrompter.Instance.QoLText(inQoLRange);
        }
    }

    public void ReloadScene()
    {
        Time.timeScale = 1f; // in case the game was paused when this runs
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}