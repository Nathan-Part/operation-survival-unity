using System.Collections;
using System.Collections.Generic;
using Game.Data;
using Game.Data.Save;
using UnityEngine;

namespace Game.Runtime
{
    /// Lance les consommables : lit le stock persistant (save.consumables), décrémente + persiste,
    /// spawn un ThrownProjectile paramétré par la fiche ConsumableData, dispatch l'effet à la détonation.
    /// Le medkit (T8) est un consommable instantané (pas de lancer) : soin direct du joueur.
    public class ConsumableThrower : MonoBehaviour
    {
        GameDatabase db; MetaProgress meta; Camera cam; WaveManager waves; PlayerHealth playerHealth;
        Dictionary<string, ConsumableData> byKey;

        /// Réglé par le Lot 3 (T24, mode hardcore) — désactive le medkit. Par défaut false.
        public bool Hardcore;

        public void Init(GameDatabase db, MetaProgress meta, Camera cam, WaveManager waves, PlayerHealth playerHealth)
        {
            this.db = db; this.meta = meta; this.cam = cam; this.waves = waves; this.playerHealth = playerHealth;
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

        /// Medkit (web #178) : consommable non lancé — soin instantané, désactivé en hardcore.
        public void UseMedkit()
        {
            if (Hardcore || !HasStock("medkit") || playerHealth == null) return; // web: medkit désactivé en hardcore
            meta.Data.consumables["medkit"]--; meta.Persist();
            playerHealth.Heal(30f); // web: MEDKIT_HEAL=30
        }

        void Detonate(string key, ConsumableData d, Vector3 point)
        {
            switch (key)
            {
                case "frag":
                case "sticky":
                    AreaDamage.Apply(waves.Alive, point, d.radius, d.damage);
                    break;
                case "molotov":
                    // Flaque de feu : visuel via GroundZoneEffect (pas de cible joueur ici),
                    // dégâts périodiques FLAT (pas de falloff) à tout ennemi dans le rayon — comme la zone toxique (EnemySpecials).
                    GroundZoneEffect.Create(point, new Color(1f, 0.5f, 0.1f, 0.35f), d.radius, d.effectSeconds, null);
                    StartCoroutine(TickFlatDamage(point, d.radius, 6, 0.5f, d.effectSeconds)); // web: MOLOTOV_TICK_DMG=6
                    break;
                case "emp":
                    foreach (var e in waves.Alive)
                        if (e != null && !e.IsDead && Vector3.Distance(point, e.transform.position) <= d.radius)
                        {
                            var c = e.GetComponent<EnemyController>();
                            if (c != null) c.Suppress(d.effectSeconds); // web: EMP_DURATION=4
                        }
                    break;
                case "flashbang":
                    foreach (var e in waves.Alive)
                        if (e != null && !e.IsDead && Vector3.Distance(point, e.transform.position) <= d.radius)
                        {
                            var c = e.GetComponent<EnemyController>();
                            if (c != null) c.Suppress(d.effectSeconds);
                        }
                    if (cam != null && Vector3.Distance(cam.transform.position, point) <= 10f) // web: FLASH_PLAYER_R=10
                        CalloutEvents.Raise("FLASH"); // le voile blanc est géré par HudScreenEffects
                    break;
                case "smoke":
                    GroundZoneEffect.Create(point, new Color(0.7f, 0.7f, 0.7f, 0.5f), d.radius, d.effectSeconds, null); // nuage visuel
                    break;
            }
        }

        /// Dégâts répétés dans un rayon, montant FLAT par tick — pas de falloff par distance
        /// (contrairement à AreaDamage/ExplosionDamage, réservé aux explosions instantanées).
        /// Mirroring EnemySpecials (zone toxique) : dégâts identiques partout dans le rayon.
        IEnumerator TickFlatDamage(Vector3 point, float radius, int tickDamage, float interval, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                yield return new WaitForSeconds(interval);
                elapsed += interval;
                if (waves == null) continue;
                var alive = waves.Alive;
                for (int i = 0; i < alive.Count; i++)
                {
                    var e = alive[i];
                    if (e == null || e.IsDead) continue;
                    if (Vector3.Distance(point, e.transform.position) <= radius) e.ApplyDamage(tickDamage);
                }
            }
        }
    }
}
