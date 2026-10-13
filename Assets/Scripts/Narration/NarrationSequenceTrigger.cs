using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum NarrationType { Scene, Action, Dialogue, Combat, Discovery, Failure, Transition, System }

[Serializable]
public class NarrationLine
{
    public NarrationType type = NarrationType.Scene;
    [Tooltip("Leave as 'narrator' for plain narration, or type a character's name.")]
    public string speaker = "narrator";
    [TextArea(2, 6)] public string text;
}

[Serializable]
public class NarrationSequence
{
    [Tooltip("Unique name, e.g. Intro, FirstCombat, FoundKey")]
    public string name;
    public bool playOnStart = false;
    public bool playOnce = true;
    public List<NarrationLine> lines = new List<NarrationLine>();
    [Tooltip("Runs when the last line is dismissed. Use it to unlock doors, start gameplay, etc.")]
    public UnityEvent onFinished;
}

// Attach to IntroManager (or any object). Type your narration in the Inspector.
public class NarrationSequencePlayer : MonoBehaviour
{
    [SerializeField] private NarrationUI ui;
    [SerializeField] private string playerName = "Player";   // fills {player_name}
    [SerializeField] private List<NarrationSequence> sequences = new List<NarrationSequence>();

    private readonly HashSet<string> played = new HashSet<string>();
    private readonly Queue<NarrationSequence> queue = new Queue<NarrationSequence>();
    private NarrationSequence active;
    private int index;

    private void Awake()
    {
        ui.OnContinue += Advance;
    }

    private void OnDestroy()
    {
        if (ui != null) ui.OnContinue -= Advance;
    }

    private void Start()
    {
        foreach (var s in sequences)
            if (s.playOnStart) Play(s.name);
    }

    // Call from code, a UnityEvent (button, trigger, onFinished...) or NarrationSequenceTrigger.
    public void Play(string sequenceName)
    {
        var seq = sequences.Find(s => s.name == sequenceName);
        if (seq == null)
        {
            Debug.LogWarning($"[Narration] No sequence named '{sequenceName}'.");
            return;
        }

        if (seq.playOnce && played.Contains(seq.name)) return;
        played.Add(seq.name);

        if (active != null) queue.Enqueue(seq);   // wait for the current one to finish
        else Begin(seq);
    }

    private void Begin(NarrationSequence seq)
    {
        active = seq;
        index = 0;
        if (seq.lines.Count == 0) { Finish(); return; }
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        var line = active.lines[index];
        var panel = new NarrationPanel
        {
            id = $"{active.name}_{index}",
            type = line.type.ToString().ToLower(),
            speaker = line.speaker
        };
        ui.Show(panel, Format(line.text), new List<NarrationChoice>());
    }

    private void Advance()
    {
        if (active == null) return;
        index++;
        if (index >= active.lines.Count) Finish();
        else ShowCurrent();
    }

    private void Finish()
    {
        var done = active;
        active = null;
        ui.Hide();
        done.onFinished?.Invoke();

        if (queue.Count > 0) Begin(queue.Dequeue());
    }

    private string Format(string text)
    {
        return (text ?? "").Replace("{player_name}", playerName);
    }
}