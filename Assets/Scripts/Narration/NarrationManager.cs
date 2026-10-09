using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace Sushi.Narration
{
    /// <summary>
    /// Plays NarrationData assets in a text box. One per scene.
    /// Call NarrationManager.Instance.Play(data) from anywhere, or use NarrationTrigger.
    /// Also pauses any scripts you list (player movement, fishing input) while narration plays.
    /// </summary>
    public class NarrationManager : MonoBehaviour
    {
        public static NarrationManager Instance { get; private set; }
        public static bool IsPlaying => Instance != null && Instance.playing;

        [Header("UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [Tooltip("Optional 'press to continue' object, shown when a line is fully revealed.")]
        [SerializeField] private GameObject continueIndicator;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource voiceSource;

        [Header("Behaviour")]
        [SerializeField, Min(1f)] private float charsPerSecond = 40f;
        [SerializeField] private Key advanceKey = Key.Enter;
        [SerializeField] private bool advanceWithClick = true;

        [Header("Pause while narrating")]
        [Tooltip("Drag components here: player controller, fishing input script, etc. They are switched off during narration and restored after.")]
        [SerializeField] private List<Behaviour> pauseWhilePlaying = new List<Behaviour>();

        [Header("Events")]
        public UnityEvent OnNarrationStarted;
        public UnityEvent OnNarrationFinished;

        private readonly Queue<NarrationData> queue = new Queue<NarrationData>();
        private readonly HashSet<string> played = new HashSet<string>();
        private readonly List<Behaviour> pausedByUs = new List<Behaviour>();
        private Coroutine routine;
        private bool playing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (panel != null) panel.SetActive(false);
        }

        private void OnDisable()
        {
            ResumeScripts();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Plays a narration, or queues it if one is already playing (unless interrupt is true).
        /// Returns false if skipped (empty, or play-once and already played).
        /// </summary>
        public bool Play(NarrationData data, bool interrupt = false)
        {
            if (data == null || data.lines == null || data.lines.Length == 0) return false;
            if (data.playOnce && played.Contains(data.Key)) return false;

            if (playing && !interrupt)
            {
                queue.Enqueue(data);
                return true;
            }

            if (routine != null) StopCoroutine(routine);
            if (interrupt) queue.Clear();

            routine = StartCoroutine(PlayRoutine(data));
            return true;
        }

        public bool HasPlayed(NarrationData data) => data != null && played.Contains(data.Key);

        public void ResetPlayed() => played.Clear();

        public void StopAll()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            queue.Clear();
            Finish();
        }

        private IEnumerator PlayRoutine(NarrationData data)
        {
            if (!playing)
            {
                playing = true;
                PauseScripts();
                if (panel != null) panel.SetActive(true);
                OnNarrationStarted?.Invoke();
            }

            if (data.playOnce) played.Add(data.Key);

            // Skip a frame so the input that started this isn't read as "advance".
            yield return null;

            for (int i = 0; i < data.lines.Length; i++)
            {
                NarrationData.Line line = data.lines[i];
                if (line == null) continue;

                if (speakerLabel != null)
                {
                    speakerLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(line.speaker));
                    speakerLabel.text = line.speaker;
                }

                if (continueIndicator != null) continueIndicator.SetActive(false);

                if (voiceSource != null && line.voice != null)
                {
                    voiceSource.Stop();
                    voiceSource.PlayOneShot(line.voice);
                }

                yield return RevealLine(line.text);

                if (continueIndicator != null) continueIndicator.SetActive(true);
                while (!AdvancePressed()) yield return null;
                yield return null;
            }

            routine = null;

            if (queue.Count > 0)
            {
                routine = StartCoroutine(PlayRoutine(queue.Dequeue()));
                yield break;
            }

            Finish();
        }

        private IEnumerator RevealLine(string text)
        {
            if (bodyLabel == null) yield break;

            bodyLabel.text = text;
            bodyLabel.ForceMeshUpdate();
            int total = bodyLabel.textInfo.characterCount;
            bodyLabel.maxVisibleCharacters = 0;

            float shown = 0f;
            while (shown < total)
            {
                // A press while typing reveals the whole line instead of advancing.
                if (AdvancePressed()) break;

                shown += charsPerSecond * Time.unscaledDeltaTime;
                bodyLabel.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                yield return null;
            }

            bodyLabel.maxVisibleCharacters = total;
            yield return null;
        }

        private bool AdvancePressed()
        {
            if (Keyboard.current != null && Keyboard.current[advanceKey].wasPressedThisFrame) return true;
            if (advanceWithClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            return false;
        }

        private void PauseScripts()
        {
            for (int i = 0; i < pauseWhilePlaying.Count; i++)
            {
                Behaviour b = pauseWhilePlaying[i];
                // Only touch scripts that are on, so nothing disabled on purpose gets switched back on.
                if (b != null && b.enabled && !pausedByUs.Contains(b))
                {
                    b.enabled = false;
                    pausedByUs.Add(b);
                }
            }
        }

        private void ResumeScripts()
        {
            for (int i = 0; i < pausedByUs.Count; i++)
                if (pausedByUs[i] != null) pausedByUs[i].enabled = true;

            pausedByUs.Clear();
        }

        private void Finish()
        {
            playing = false;
            ResumeScripts();
            if (panel != null) panel.SetActive(false);
            if (continueIndicator != null) continueIndicator.SetActive(false);
            OnNarrationFinished?.Invoke();
        }
    }
}