using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NarrationUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject root;              // the whole narration panel
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button continueButton;        // also used to skip typing

    [Header("Choices")]
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private Button choiceButtonPrefab;    // needs a TMP_Text child

    [Header("Typewriter")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float charsPerSecond = 40f;

    public event Action OnContinue;
    public event Action<NarrationChoice> OnChoiceSelected;

    private Coroutine typing;
    private bool isTyping;
    private string fullText;
    private List<NarrationChoice> pendingChoices = new List<NarrationChoice>();

    private void Awake()
    {
        continueButton.onClick.AddListener(HandleContinueClicked);
    }

    public void Show(NarrationPanel panel, string text, List<NarrationChoice> choices)
    {
        root.SetActive(true);
        ClearChoices();

        bool hasSpeaker = !string.IsNullOrEmpty(panel.speaker) && panel.speaker != "narrator";
        speakerText.gameObject.SetActive(hasSpeaker);
        if (hasSpeaker) speakerText.text = panel.speaker;

        fullText = text;
        pendingChoices = choices;
        continueButton.gameObject.SetActive(false);

        if (typing != null) StopCoroutine(typing);

        if (useTypewriter) typing = StartCoroutine(Type());
        else FinishTyping();
    }

    public void Hide()
    {
        if (typing != null) StopCoroutine(typing);
        ClearChoices();
        root.SetActive(false);
    }

    private IEnumerator Type()
    {
        isTyping = true;
        continueButton.gameObject.SetActive(true); // lets the player tap to skip
        bodyText.text = fullText;
        bodyText.maxVisibleCharacters = 0;

        float delay = 1f / Mathf.Max(1f, charsPerSecond);
        for (int i = 0; i <= fullText.Length; i++)
        {
            bodyText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }

        FinishTyping();
    }

    private void FinishTyping()
    {
        if (typing != null) StopCoroutine(typing);
        typing = null;
        isTyping = false;

        bodyText.text = fullText;
        bodyText.maxVisibleCharacters = int.MaxValue;

        if (pendingChoices.Count > 0)
        {
            continueButton.gameObject.SetActive(false);
            foreach (var choice in pendingChoices)
            {
                var btn = Instantiate(choiceButtonPrefab, choiceContainer);
                btn.GetComponentInChildren<TMP_Text>().text = choice.label;
                var captured = choice;
                btn.onClick.AddListener(() => OnChoiceSelected?.Invoke(captured));
            }
        }
        else
        {
            continueButton.gameObject.SetActive(true);
        }
    }

    private void HandleContinueClicked()
    {
        if (isTyping) FinishTyping();   // first click completes the text
        else OnContinue?.Invoke();      // second click advances
    }

    private void ClearChoices()
    {
        foreach (Transform child in choiceContainer)
            Destroy(child.gameObject);
    }
}