using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    /// Câblage runtime de la série de points loadout (web #156) : accumule les points sur kill,
    /// et déclenche le gadget correspondant (spawn branché par SliceBootstrap, T13-T15) quand la
    /// touche est pressée et que la jauge est prête.
    public class LoadoutController : MonoBehaviour
    {
        public LoadoutModel Model { get; private set; }
        readonly Dictionary<string, Action> triggers = new Dictionary<string, Action>();

        public void Init(IReadOnlyList<string> selected, IReadOnlyDictionary<string, int> thresholds, WaveManager waves)
        { Model = new LoadoutModel(selected, thresholds); waves.EnemyKilled += (_, __) => Model.OnKill(); }

        public void SetTrigger(string key, Action spawn) => triggers[key] = spawn; // branché par SliceBootstrap (T13-15)

        public void TryTrigger(string key)
        {
            if (Model == null || !triggers.ContainsKey(key)) return;
            if (Model.TryConsume(key)) triggers[key]?.Invoke();
        }
    }
}
