using System;
using System.Collections;
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
    [Tooltip("Seconds to stay on screen after the text finishes. 0 = use the default.")]
    public float holdSeconds = 0f;
}

[Serializable]
public class NarrationSequence
{
    [Tooltip("Unique name, e.g. Intro, FirstCatch, LineSnap")]
    public string name;
    public bool playOnStart = false;
    public bool playOnce = true;
    public List<NarrationLine> lines = new List<NarrationLine>();
    [Tooltip("Runs after the last line disappears.")]
    public UnityEvent onFinished;
}

// Attach to IntroManager. Type your narration in the Inspector.
// Each line appears, stays for a few seconds, then disappears by itself.
public class NarrationSequencePlayer : MonoBehaviour
{
    [SerializeField] private NarrationUI ui;
    [SerializeField] private string playerName = "Player";    // fills {player_name}
    [SerializeField] private float defaultHoldSeconds = 2.5f; // time on screen after typing finishes
    [SerializeField] private float gapBetweenLines = 0.3f;
    [Tooltip("Minimum seconds between the end of one sequence and the start of the next.")]
    [SerializeField] private float minSecondsBetweenSequences = 6f;
    [SerializeField] private List<NarrationSequence> sequences = new List<NarrationSequence>();

    private readonly HashSet<string> played = new HashSet<string>();
    private readonly Queue<NarrationSequence> queue = new Queue<NarrationSequence>();
    private bool running;
    private float lastSequenceEndTime = -999f;

    private void Start()
    {
        foreach (var s in sequences)
            if (s.playOnStart) Play(s.name);
    }

    // Call from code, a UnityEvent, or NarrationSequenceTrigger.
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

        queue.Enqueue(seq);
        if (!running) StartCoroutine(RunQueue());
    }

    private IEnumerator RunQueue()
    {
        running = true;

        while (queue.Count > 0)
        {
            var seq = queue.Dequeue();

            // Wait until enough time has passed since the previous sequence ended.
            float wait = lastSequenceEndTime + minSecondsBetweenSequences - Time.time;
            if (wait > 0f) yield return new WaitForSeconds(wait);

            foreach (var line in seq.lines)
            {
                float typeTime = ui.Show(line.speaker, (line.text ?? "").Replace("{player_name}", playerName));
                float hold = line.holdSeconds > 0f ? line.holdSeconds : defaultHoldSeconds;
                yield return new WaitForSeconds(typeTime + hold);

                ui.Hide();
                yield return new WaitForSeconds(gapBetweenLines);
            }

            lastSequenceEndTime = Time.time;
            seq.onFinished?.Invoke();
        }

        running = false;
    }
}