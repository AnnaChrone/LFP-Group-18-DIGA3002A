using UnityEngine;

// Put on a GameObject with a trigger Collider. When the Player walks in,
// the named sequence plays (once, if that sequence has Play Once ticked).
public class NarrationSequenceTrigger : MonoBehaviour
{
    [SerializeField] private NarrationSequencePlayer player;
    [SerializeField] private string sequenceName;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) player.Play(sequenceName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) player.Play(sequenceName);
    }
}