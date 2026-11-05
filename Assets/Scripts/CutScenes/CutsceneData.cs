using System;
using UnityEngine;
using UnityEngine.Events;

public enum CutsceneStepType
{
    TextLine,
    ClearText,
    ShowGraphic,
    HideGraphic,
    WaitForInput,
    TriggerEvent,
    CustomDelay,
    TeleportPlayer
}

[Serializable]
public class CutsceneStep
{
    public CutsceneStepType type;

    [Header("Text")]
    [TextArea(2, 5)] public string text;
    [Tooltip("0 = use the controller's default fade time")]
    public float textFadeTime = 0f;

    [Header("Audio (TextLine / ShowGraphic)")]
    public AudioClip audioToPlay;

    [Header("Graphic (ShowGraphic / HideGraphic)")]
    public Sprite graphicToShow;
    public float graphicFadeTime = 0.5f;
    public bool clearTextBeforeGraphic = false;

    [Header("Delay (CustomDelay)")]
    public float delayTime = 1f;

    [Header("Teleport (TeleportPlayer)")]
    public Vector2 teleportPosition;
    public string teleportSceneName = "";

    [Header("Event (TriggerEvent)")]
    public UnityEvent onStepTrigger;
}

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Cutscenes/Cutscene Data")]
public class CutsceneData : ScriptableObject
{
    [Tooltip("Picture shown behind the text for this whole cutscene.")]
    public Sprite backgroundSprite;

    [Header("Text Position (fractions of the screen: 0,0 = bottom-left, 1,1 = top-right)")]
    [Tooltip("Bottom-left corner of the area the text should fill (e.g. inside the speech bubble).")]
    public Vector2 textAreaMin = new Vector2(0.2f, 0.2f);
    [Tooltip("Top-right corner of the area the text should fill.")]
    public Vector2 textAreaMax = new Vector2(0.8f, 0.8f);

    public CutsceneStep[] steps;

    // Note: events stored in an asset can only call other assets, not scene objects.
    // For scene objects, use onAnyCutsceneEnd on the CutsceneController instead.
    public UnityEvent onCutsceneEnd;
}