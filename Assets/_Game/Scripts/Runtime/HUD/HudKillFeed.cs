using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Kill feed (web #54) : liste haut droite, couleur = radarColor de l'ennemi.
    public class HudKillFeed : MonoBehaviour
    {
        readonly KillFeedModel model = new KillFeedModel();
        Text[] lines;

        public void Init(RectTransform root, WeaponController weapon)
        {
            lines = new Text[KillFeedModel.MaxEntries];
            for (int i = 0; i < lines.Length; i++)
            {
                var line = HudBuild.MakeText(root, "", 24, TextAnchor.UpperRight, "KillFeedLine");
                var rect = line.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-24f, -180f - i * 30f); // sous le radar
                rect.sizeDelta = new Vector2(420f, 28f);
                lines[i] = line;
            }
            weapon.HitConfirmed += OnHit;
        }

        void OnHit(HitZone zone, bool killed, EnemyHealth target)
        {
            if (!killed || target == null || target.Data == null) return;
            var data = target.Data;
            model.Push(data.displayName, zone == HitZone.Head, data.id == "boss", EnemySpawner.RadarColor(data));
        }

        void Update()
        {
            model.Tick(Time.deltaTime);
            for (int i = 0; i < lines.Length; i++)
            {
                if (i >= model.Entries.Count) { lines[i].text = ""; continue; }
                var entry = model.Entries[i];
                var color = entry.color;
                color.a = KillFeedModel.Alpha(entry.age);
                lines[i].text = entry.text;
                lines[i].color = color;
            }
        }
    }
}
