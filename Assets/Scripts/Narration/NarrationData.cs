using System;
using UnityEngine;

namespace Sushi.Narration
{
    /// <summary>
    /// One piece of narration: a short sequence of lines. Create these in the
    /// Project window (Create > Sushi > Narration) and drag them onto triggers,
    /// targets or anything else that wants to play narration.
    /// </summary>
    [CreateAssetMenu(fileName = "Narration_New", menuName = "Sushi/Narration")]
    public class NarrationData : ScriptableObject
    {
        [Serializable]
        public class Line
        {
            [Tooltip("Leave blank for pure narration with no speaker name.")]
            public string speaker;

            [TextArea(2, 5)]
            public string text;

            [Tooltip("Optional voice clip or sound effect played when the line starts.")]
            public AudioClip voice;
        }

        [Tooltip("Stable key used to remember that this was already played. Leave blank to use the asset name.")]
        public string id;

        [Tooltip("If ticked, this only plays once per session.")]
        public bool playOnce = true;

        public Line[] lines;

        public string Key => string.IsNullOrWhiteSpace(id) ? name : id;
    }
}