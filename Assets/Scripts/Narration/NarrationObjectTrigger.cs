using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Sushi.Narration
{
    /// <summary>
    /// One component for every way of starting narration. Pick a Mode:
    ///  - OnEnter:       player walks into a trigger collider (Collider2D, Is Trigger)
    ///  - OnInteractKey: player is inside the trigger and presses the interact key
    ///  - OnWorldClick:  player clicks this object (needs a Collider2D, any kind)
    ///  - OnUIClick:     player clicks this UI element (needs a Graphic with Raycast Target on)
    /// Also has a public Play() so buttons and UnityEvents can call it.
    /// </summary>
    public class NarrationTrigger : MonoBehaviour, IPointerClickHandler
    {
        public enum Mode { OnEnter, OnInteractKey, OnWorldClick, OnUIClick }

        [SerializeField] private NarrationData narration;
        [SerializeField] private Mode mode = Mode.OnEnter;
        [SerializeField] private string playerTag = "Player";

        [Header("Interact key mode")]
        [SerializeField] private Key interactKey = Key.E;
        [Tooltip("Optional 'Press E' object, shown while the player is in range.")]
        [SerializeField] private GameObject promptObject;

        [Header("Click modes")]
        [SerializeField] private LayerMask clickableLayers = ~0;
        [Tooltip("The player must be within this distance to trigger a click. 0 = any distance.")]
        [SerializeField, Min(0f)] private float maxPlayerDistance = 0f;

        [Header("After playing")]
        [SerializeField] private bool disableAfterPlay = false;

        private bool playerInRange;
        private Transform player;

        private void OnEnable()
        {
            if (promptObject != null) promptObject.SetActive(false);
        }

        private void Update()
        {
            switch (mode)
            {
                case Mode.OnInteractKey: UpdateInteract(); break;
                case Mode.OnWorldClick: UpdateWorldClick(); break;
            }
        }

        private void UpdateInteract()
        {
            if (!playerInRange) return;

            if (promptObject != null)
                promptObject.SetActive(!NarrationManager.IsPlaying);

            if (Keyboard.current == null || NarrationManager.IsPlaying) return;

            if (Keyboard.current[interactKey].wasPressedThisFrame)
                Play();
        }

        private void UpdateWorldClick()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (NarrationManager.IsPlaying) return;

            // Clicks that land on UI belong to the UI.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (!PlayerClose()) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 world = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D[] hits = Physics2D.OverlapPointAll(world, clickableLayers);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform == transform || hits[i].transform.IsChildOf(transform))
                {
                    Play();
                    return;
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (mode != Mode.OnUIClick || NarrationManager.IsPlaying) return;
            Play();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag(playerTag)) return;

            playerInRange = true;
            if (mode == Mode.OnEnter) Play();
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!collision.CompareTag(playerTag)) return;

            playerInRange = false;
            if (promptObject != null) promptObject.SetActive(false);
        }

        public void Play()
        {
            if (NarrationManager.Instance == null)
            {
                Debug.LogWarning("[NarrationTrigger] No NarrationManager in the scene.", this);
                return;
            }

            bool started = NarrationManager.Instance.Play(narration);

            if (started && disableAfterPlay)
            {
                if (promptObject != null) promptObject.SetActive(false);
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
                enabled = false;
            }
        }

        private bool PlayerClose()
        {
            if (maxPlayerDistance <= 0f) return true;

            if (player == null)
            {
                GameObject found = GameObject.FindGameObjectWithTag(playerTag);
                if (found == null) return true; // no player found, don't block the click
                player = found.transform;
            }

            return Vector2.Distance(player.position, transform.position) <= maxPlayerDistance;
        }
    }
}