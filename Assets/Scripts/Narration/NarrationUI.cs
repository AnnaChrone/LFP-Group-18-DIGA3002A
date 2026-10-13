using System.Collections;
using TMPro;
using UnityEngine;

// Just two text fields. No buttons, no choices.
public class NarrationUI : MonoBehaviour
{
    [SerializeField] private GameObject root;        // the whole narration box
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Typewriter")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float charsPerSecond = 40f;

    private Coroutine typing;

    private void Awake()
    {
        root.SetActive(false);
    }

    // Shows a line. Returns how long (seconds) the text takes to finish appearing.
    public float Show(string speaker, string text)
    {
        root.SetActive(true);

        bool hasSpeaker = !string.IsNullOrEmpty(speaker) && speaker.ToLower() != "narrator";
        speakerText.gameObject.SetActive(hasSpeaker);
        if (hasSpeaker) speakerText.text = speaker;

        bodyText.text = text;

        if (typing != null) StopCoroutine(typing);

        if (!useTypewriter)
        {
            bodyText.maxVisibleCharacters = int.MaxValue;
            return 0f;
        }

        typing = StartCoroutine(Type(text.Length));
        return text.Length / Mathf.Max(1f, charsPerSecond);
    }

    public void Hide()
    {
        if (typing != null) StopCoroutine(typing);
        typing = null;
        root.SetActive(false);
    }

    private IEnumerator Type(int length)
    {
        float delay = 1f / Mathf.Max(1f, charsPerSecond);
        for (int i = 0; i <= length; i++)
        {
            bodyText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }
        bodyText.maxVisibleCharacters = int.MaxValue;
        typing = null;
    }
}