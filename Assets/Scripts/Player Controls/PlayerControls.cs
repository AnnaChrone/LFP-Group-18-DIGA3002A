using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private int facingDirection = 1;

    [Header("Fishing Variables")]
    public FishingManager fishingManager;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;

        if (moveInput.x > 0 && facingDirection == -1 ||
            moveInput.x < 0 && facingDirection == 1)
        {
            //Flip();
        }
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
        if (fishingManager.busyFishing == false)
        {
            moveInput = context.ReadValue<Vector2>();

        }
    }


    public void OnInteract(InputAction.CallbackContext context)
    {
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
}