using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    /// Série de points (web #156) : 100 pts/kill sur chacun des bonus sélectionnés ; prêt au seuil ;
    /// déclencher remet la jauge à 0.
    public class LoadoutModel
    {
        public const int PointsPerKill = 100; // web #156 : 100 pts par kill, quel que soit l'ennemi
        readonly IReadOnlyList<string> selected;
        readonly IReadOnlyDictionary<string, int> thresholds;
        readonly Dictionary<string, int> points = new Dictionary<string, int>();

        public LoadoutModel(IReadOnlyList<string> selected, IReadOnlyDictionary<string, int> thresholds)
        { this.selected = selected; this.thresholds = thresholds; foreach (var k in selected) points[k] = 0; }

        public IReadOnlyList<string> Selected => selected;
        public void OnKill() { foreach (var k in selected) points[k] = points[k] + PointsPerKill; }
        int Threshold(string k) => thresholds.TryGetValue(k, out int t) ? t : int.MaxValue;
        public bool IsReady(string k) => points.TryGetValue(k, out int p) && p >= Threshold(k);
        public float Fill(string k) => points.TryGetValue(k, out int p) ? Mathf.Clamp01((float)p / Threshold(k)) : 0f;
        public bool TryConsume(string k) { if (!IsReady(k)) return false; points[k] = 0; return true; }
    }
}
