using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// File de toasts méta (succès/contrats/niveau, Lot 6) : haut-centre, 2.6 s chacun,
    /// affichés séquentiellement pour ne jamais en perdre si plusieurs events arrivent
    /// la même frame (ex. plusieurs succès déverrouillés d'un coup). Patron HudCallout.
    public class HudToasts : MonoBehaviour
    {
        const float ShowSeconds = 2.6f;

        readonly Queue<string> pending = new Queue<string>();
        RunTracker tracker;
        Text label;
        float hideAt = -1f;

        public void Init(RectTransform root, RunTracker tracker)
        {
            this.tracker = tracker;
            label = HudBuild.MakeText(root, "", 32, TextAnchor.MiddleCenter, "Toast");
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -140f);
            rect.sizeDelta = new Vector2(900f, 60f);
            label.color = new Color(0.6f, 1f, 0.7f);
            label.gameObject.SetActive(false);
            if (tracker != null) tracker.Toast += Enqueue;
        }

        void OnDestroy()
        {
            if (tracker != null) tracker.Toast -= Enqueue;
        }

        void Enqueue(string text)
        {
            if (!string.IsNullOrEmpty(text)) pending.Enqueue(text);
        }

        void Update()
        {
            if (hideAt >= 0f && Time.unscaledTime < hideAt) return;
            if (hideAt >= 0f)
            {
                label.gameObject.SetActive(false);
                hideAt = -1f;
            }
            if (pending.Count == 0) return;
            label.text = pending.Dequeue();
            label.gameObject.SetActive(true);
            hideAt = Time.unscaledTime + ShowSeconds;
        }
    }
}
