using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NarrationManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextAsset scriptFile;   // drag panels.json here
    [SerializeField] private NarrationUI ui;

    [Header("Options")]
    [SerializeField] private bool playOnStart = true;

    // Hooks for the rest of your game (music, backgrounds, saving...)
    public event Action<NarrationPanel> OnPanelShown;
    public event Action<string> OnLocationChanged;
    public event Action OnNarrationEnded;

    private NarrationScript script;
    private readonly Dictionary<string, NarrationPanel> lookup = new Dictionary<string, NarrationPanel>();
    private readonly HashSet<string> flags = new HashSet<string>();
    private readonly Dictionary<string, string> variables = new Dictionary<string, string>();

    private NarrationPanel current;
    private List<NarrationChoice> visibleChoices = new List<NarrationChoice>();
    private string lastLocation;

    private void Awake()
    {
        script = JsonUtility.FromJson<NarrationScript>(scriptFile.text);
        foreach (var p in script.panels)
            lookup[p.id] = p;

        Validate();

        ui.OnContinue += Continue;
        ui.OnChoiceSelected += Choose;
    }

    private void Start()
    {
        if (playOnStart) Play(script.meta.start_panel);
    }

    // ---------- Public API ----------

    public void SetVariable(string key, string value) => variables[key] = value;
    public void SetFlag(string flag) => flags.Add(flag);
    public void ClearFlag(string flag) => flags.Remove(flag);
    public bool HasFlag(string flag) => flags.Contains(flag);

    public void Play(string id)
    {
        if (string.IsNullOrEmpty(id) || id == "END")
        {
            End();
            return;
        }

        if (!lookup.TryGetValue(id, out var panel))
        {
            Debug.LogError($"[Narration] Panel '{id}' not found.");
            End();
            return;
        }

        // Panel's conditions not met: skip it and follow its 'next'.
        if (!CheckAll(panel.conditions))
        {
            Play(panel.next);
            return;
        }

        current = panel;
        foreach (var f in panel.sets) flags.Add(f);

        visibleChoices = panel.choices.Where(c => CheckAll(c.requires)).ToList();

        if (panel.location != lastLocation)
        {
            lastLocation = panel.location;
            OnLocationChanged?.Invoke(panel.location);
        }

        ui.Show(panel, Format(panel.text), visibleChoices);
        OnPanelShown?.Invoke(panel);
    }

    // ---------- Flow ----------

    private void Continue()
    {
        if (current == null || visibleChoices.Count > 0) return;
        Play(current.next);
    }

    private void Choose(NarrationChoice choice)
    {
        Play(choice.next);
    }

    private void End()
    {
        current = null;
        ui.Hide();
        OnNarrationEnded?.Invoke();
    }

    // ---------- Helpers ----------

    // Conditions: "flag" must be set, "!flag" must NOT be set.
    private bool CheckAll(List<string> conds)
    {
        if (conds == null) return true;
        foreach (var c in conds)
        {
            if (string.IsNullOrEmpty(c)) continue;
            bool negate = c.StartsWith("!");
            string flag = negate ? c.Substring(1) : c;
            if (flags.Contains(flag) == negate) return false;
        }
        return true;
    }

    // Replaces {player_name}-style tokens.
    private string Format(string text)
    {
        foreach (var kv in variables)
            text = text.Replace("{" + kv.Key + "}", kv.Value);
        return text;
    }

    // Catches script mistakes on load.
    private void Validate()
    {
        var seen = new HashSet<string>();
        var referenced = new HashSet<string> { script.meta.start_panel };

        foreach (var p in script.panels)
        {
            if (!seen.Add(p.id))
                Debug.LogWarning($"[Narration] Duplicate panel id '{p.id}'.");

            if (!string.IsNullOrEmpty(p.next)) referenced.Add(p.next);
            foreach (var c in p.choices) referenced.Add(c.next);

            if (p.choices.Count == 0 && string.IsNullOrEmpty(p.next))
                Debug.LogWarning($"[Narration] Dead end: '{p.id}' has no next or choices.");
        }

        foreach (var target in referenced)
            if (target != "END" && !lookup.ContainsKey(target))
                Debug.LogWarning($"[Narration] Missing target panel '{target}'.");

        foreach (var p in script.panels)
            if (!referenced.Contains(p.id))
                Debug.LogWarning($"[Narration] Orphan panel: '{p.id}' is never referenced.");
    }
}