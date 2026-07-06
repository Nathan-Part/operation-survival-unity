using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    /// Journal de kills (web #54 : max 5 entrées, vie 4 s, fondu 0.5 s).
    public class KillFeedModel
    {
        public const int MaxEntries = 5;    // web: KF_MAX
        public const float Life = 4f;       // web: KF_LIFE
        public const float Fade = 0.5f;     // web: KF_FADE

        public struct Entry
        {
            public string text;
            public Color color;
            public float age;
        }

        readonly List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        public void Push(string label, bool headshot, bool boss, Color color)
        {
            string prefix = boss ? "BOSS DOWN · " : headshot ? "HEADSHOT · " : "";
            entries.Insert(0, new Entry { text = "☠ " + prefix + label, color = color, age = 0f });
            if (entries.Count > MaxEntries) entries.RemoveAt(entries.Count - 1);
        }

        public void Tick(float dt)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                e.age += dt;
                if (e.age >= Life + Fade) entries.RemoveAt(i);
                else entries[i] = e;
            }
        }

        public static float Alpha(float age) =>
            age <= Life ? 1f : Mathf.Clamp01(1f - (age - Life) / Fade);
    }
}
