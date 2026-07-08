using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Liste des power-ups actifs (web #34+), en haut à gauche : id + temps restant, mise à jour live.
    public class HudPowerUps : MonoBehaviour
    {
        const int MaxLines = 6;

        PowerUpRuntime powerups;
        Text[] lines;
        readonly List<KeyValuePair<string, float>> buffer = new List<KeyValuePair<string, float>>();

        public void Init(RectTransform root, PowerUpRuntime powerups)
        {
            this.powerups = powerups;
            lines = new Text[MaxLines];
            for (int i = 0; i < MaxLines; i++)
            {
                var text = HudBuild.MakeText(root, "", 22, TextAnchor.UpperLeft, $"PowerUpLine_{i}");
                var rect = text.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(40f, -40f - i * 26f);
                rect.sizeDelta = new Vector2(360f, 24f);
                text.color = new Color(1f, 1f, 1f, 0.9f);
                text.gameObject.SetActive(false);
                lines[i] = text;
            }
        }

        void Update()
        {
            if (powerups == null) return;
            buffer.Clear();
            foreach (var kv in powerups.Active)
            {
                float remaining = kv.Value - Time.time;
                if (remaining > 0f) buffer.Add(new KeyValuePair<string, float>(kv.Key, remaining));
            }
            buffer.Sort((a, b) => b.Value.CompareTo(a.Value)); // plus longue durée restante en premier

            for (int i = 0; i < MaxLines; i++)
            {
                bool show = i < buffer.Count;
                if (lines[i].gameObject.activeSelf != show) lines[i].gameObject.SetActive(show);
                if (show) lines[i].text = $"{buffer[i].Key} — {buffer[i].Value:0.0}s";
            }
        }
    }
}
