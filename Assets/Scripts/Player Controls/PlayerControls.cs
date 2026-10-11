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


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (GamePaused) return;
        if (fishingManager.busyFishing)
        {
            rb.linearVelocity = Vector2.zero;
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
        if (GamePaused) return;

        if (fishingManager.busyFishing == false)
        {
            moveInput = context.ReadValue<Vector2>();

        } else
        {
            moveInput = new Vector2(0f, 0f);
            moveSpeed = 0f;
            rb.linearVelocity = Vector2.zero; 

        }
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
        pauseFunctions.togglePauseMenu(GamePaused);
        if (GamePaused)
        {
            Time.timeScale = 0f;

        }
        else
        {
            Time.timeScale = 1f;

        }

    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Stove"))
        {
            cooking.OpenStoveUI();
            Debug.Log("Opening stove");
        } else if (other.CompareTag("Serve"))
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