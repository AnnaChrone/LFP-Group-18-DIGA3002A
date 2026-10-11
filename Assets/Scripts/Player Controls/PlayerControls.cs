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

        // normal movement
        rb.linearVelocity = moveInput * moveSpeed;

        SoundManager.Instance.SetWalking(moveInput != Vector2.zero);
    }

    /*void Flip() WHEN WE HAVE A SPRITE
    {
        facingDirection *= -1;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }*/

    public void OnMovement(InputAction.CallbackContext context)
    {
        // Always store the raw input. Gating happens in FixedUpdate.
        moveInput = context.ReadValue<Vector2>();
    }


    public void OnInteract(InputAction.CallbackContext context)
    {
        if (GamePaused) return;

        if (!fishingManager.inFishingRange)
        {
            return;
        }

        if (context.started)
        {
            fishingManager.InteractPressed();
        }
    }

    public void OnFish(InputAction.CallbackContext context)
    {
        if (GamePaused) return;

        if (!fishingManager.inFishingRange)
        {
            return;
        }

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
            cooking.OpenStoveUI();
            Debug.Log("Opening stove");
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
            cooking.CloseStoveUI();
            Debug.Log("Closing stove");

        }
        else if (other.CompareTag("Serve"))
        {
            serveStation.CloseServeButton();
            Debug.Log("closing serve");
        }
    }
}