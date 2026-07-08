using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    /// Power-ups (web #34+) : effets temporisés + instantanés, ramassés sur des orbes lâchés par les
    /// ennemis (DropChance=0.12, PickupR=1.6m, durée défaut 8s). Composant unique posé sur le joueur
    /// (cf. SliceBootstrap) ; les multiplicateurs sont lus par les autres systèmes chaque frame
    /// (WeaponController, PlayerMotor, KillstreakController → PlayerHealth.incomingMult).
    /// Ce lot livre 9 effets prioritaires ; les 15 autres (descriptifs) sont un suivi (voir plan Lot 5 Step 7) :
    /// ils sont acceptés par Activate/dispatchés dans le dictionnaire de timers sans effet concret pour l'instant
    /// (aucun crash, aucun effet erroné — juste "pas encore implémenté").
    public class PowerUpRuntime : MonoBehaviour
    {
        public const float DropChance = 0.12f, PickupR = 1.6f, DefaultDuration = 8f; // web: DROP_CHANCE/PICKUP_R/durée défaut

        readonly Dictionary<string, float> until = new Dictionary<string, float>(); // id → instant d'expiration (Time.time)

        public PlayerHealth health;
        public WaveManager waves;

        public float DamageMult => Has("dmgx2") ? 2f : 1f;
        public float CooldownMult => Has("rapid") ? 1f / 1.8f : 1f;         // web: cadence × 1.8 → cooldown / 1.8
        public float MoveMult => Has("speed_boost") ? 2f : 1f;
        public float IncomingMult => Has("kevlar") ? 0.7f : 1f;             // compose avec le ×0.5 killstreak (KillstreakController)
        public float EnemySpeedMult => Has("slowmo") ? 0.4f : 1f;          // web: ennemis ralentis à 40%

        bool Has(string id) => until.TryGetValue(id, out float t) && Time.time < t;

        /// Active un effet : temporisé (stocke l'instant d'expiration) ou instantané (dispatché tout de suite,
        /// jamais stocké). Un id inconnu (les 15 power-ups de suivi) est accepté sans effet : pas de crash.
        public void Activate(string id, float duration, bool instant)
        {
            if (instant) { Instant(id); return; }
            until[id] = Time.time + (duration > 0f ? duration : DefaultDuration);
        }

        void Instant(string id)
        {
            switch (id)
            {
                case "instant_heal": health?.Heal(40f); break;                                                     // web: INSTANT_HEAL_AMOUNT=40
                case "shield_orb": health?.shield?.Refill(); break;                                                 // web: bouclier rechargé au max
                case "nova_blast": if (waves != null) AreaDamage.Apply(waves.Alive, transform.position, 8f, 80f); break; // web: NOVA_RADIUS=8, NOVA_DAMAGE=80
                // default : id instantané non prioritaire (second_wind_refill...) — suivi Step 7, aucun effet pour l'instant.
            }
        }

        /// Effets actifs (id, instant d'expiration Time.time) — le HUD calcule le temps restant.
        public IEnumerable<KeyValuePair<string, float>> Active => until;
    }
}
