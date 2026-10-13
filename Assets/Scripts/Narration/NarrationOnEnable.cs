using UnityEngine;

// Put this on any object that gets turned on during the game (a panel, a popup, a room).
// The moment it becomes active, the named narration sequence plays.
public class NarrateOnEnable : MonoBehaviour
{
    [SerializeField] private string sequenceName;

    private void OnEnable()
    {
        var player = FindFirstObjectByType<NarrationSequencePlayer>();
        if (player == null)
        {
            Debug.LogWarning("[Narration] No NarrationSequencePlayer found in the scene.");
            return;
        }

        player.Play(sequenceName);
    }
}