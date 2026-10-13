using UnityEngine;

// Put this on any GameObject. It starts a narration sequence once.
public class NarrationTrigger : MonoBehaviour
{
    public enum Mode { OnSceneStart, OnPlayerEnter }

    [SerializeField] private NarrationManager manager;
    [SerializeField] private string panelId = "intro_001";
    [SerializeField] private Mode mode = Mode.OnSceneStart;

    [Header("Play once")]
    [SerializeField] private bool playOnce = true;
    [Tooltip("If on, it also stays 'played' after you quit and relaunch the game.")]
    [SerializeField] private bool rememberBetweenSessions = false;

    private bool played;
    private string Key => "narration_played_" + panelId;

    private void Start()
    {
        if (rememberBetweenSessions && PlayerPrefs.GetInt(Key, 0) == 1)
            played = true;

        if (mode == Mode.OnSceneStart)
            Fire();
    }

    // Needs a Collider on this object with "Is Trigger" ticked,
    // and your player tagged "Player".
    private void OnTriggerEnter(Collider other)
    {
        if (mode == Mode.OnPlayerEnter && other.CompareTag("Player")) Fire();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (mode == Mode.OnPlayerEnter && other.CompareTag("Player")) Fire();
    }

    private void Fire()
    {
        if (playOnce && played) return;
        played = true;

        if (rememberBetweenSessions)
        {
            PlayerPrefs.SetInt(Key, 1);
            PlayerPrefs.Save();
        }

        manager.Play(panelId);
    }

    // Right-click the component header in the Inspector to reset while testing.
    [ContextMenu("Reset played state")]
    private void ResetPlayed()
    {
        played = false;
        PlayerPrefs.DeleteKey(Key);
    }
}