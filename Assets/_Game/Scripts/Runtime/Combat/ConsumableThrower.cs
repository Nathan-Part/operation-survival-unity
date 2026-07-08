using System.Collections.Generic;
using Game.Data;
using Game.Data.Save;
using UnityEngine;

namespace Game.Runtime
{
    /// Lance les consommables : lit le stock persistant (save.consumables), décrémente + persiste,
    /// spawn un ThrownProjectile paramétré par la fiche ConsumableData, dispatch l'effet à la détonation.
    public class ConsumableThrower : MonoBehaviour
    {
        GameDatabase db; MetaProgress meta; Camera cam; WaveManager waves;
        Dictionary<string, ConsumableData> byKey;

        public void Init(GameDatabase db, MetaProgress meta, Camera cam, WaveManager waves)
        {
            this.db = db; this.meta = meta; this.cam = cam; this.waves = waves;
            byKey = new Dictionary<string, ConsumableData>();
            foreach (var c in db.consumables) if (c != null && !string.IsNullOrEmpty(c.key)) byKey[c.key] = c;
        }

        public bool HasStock(string key) =>
            meta.Data.consumables.TryGetValue(key, out int n) && n > 0;

        public void Throw(string key)
        {
            if (!HasStock(key) || !byKey.TryGetValue(key, out var data) || cam == null) return;
            meta.Data.consumables[key]--; meta.Persist();

            var origin = cam.transform.position + cam.transform.forward * 0.5f;
            float grav = key == "frag" ? 14f : 9.81f; // web: GRENADE_GRAV=14, autres ~9.81
            ThrownProjectile.Spawn(origin, cam.transform.forward, data.throwSpeed, grav, Mathf.Max(0.01f, data.fuseSeconds),
                point => Detonate(key, data, point), data.icon != null ? null : null);
        }

        void Detonate(string key, ConsumableData d, Vector3 point)
        {
            switch (key)
            {
                case "frag":
                case "sticky":
                    AreaDamage.Apply(waves.Alive, point, d.radius, d.damage);
                    break;
                // molotov/emp/flashbang/smoke : branchés au Task 8
            }
        }
    }
}
