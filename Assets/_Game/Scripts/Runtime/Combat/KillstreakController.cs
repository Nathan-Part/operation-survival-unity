using UnityEngine;
namespace Game.Runtime
{
    /// Câblage runtime des killstreaks (web #58) : écoute les kills/dégâts, applique
    /// les récompenses temporaires (rechargement instantané / bouclier / argent ×2)
    /// et expose le compteur pour l'affichage HUD (🔥).
    public class KillstreakController : MonoBehaviour
    {
        public WaveManager waves; public WeaponController weapon; public PlayerHealth health;

        /// Power-ups (web #34+, Lot 5) : compose avec le bouclier de killstreak sur PlayerHealth.incomingMult
        /// (les deux multiplicateurs se multiplient entre eux, aucun des deux systèmes n'écrase l'autre).
        public PowerUpRuntime powerups;

        readonly KillstreakModel model = new KillstreakModel();
        float ksShieldUntil = float.NegativeInfinity, moneyMultUntil = float.NegativeInfinity;

        public int StreakCount => model.StreakCount;

        public void Init(WaveManager waves, WeaponController weapon, PlayerHealth health)
        {
            this.waves = waves; this.weapon = weapon; this.health = health;
            waves.EnemyKilled += (_, __) => OnKill();
            health.DamagedFrom += (_, __) => model.OnPlayerDamaged(); // reset au dégât
        }

        void OnKill()
        {
            int reward = model.RegisterKill(Time.time);
            if (reward == 3) { weapon.RequestReload(); CalloutEvents.Raise("TRIPLE KILL ⚡ Rechargé"); }
            else if (reward == 5) { ksShieldUntil = Time.time + 5f; CalloutEvents.Raise("RAMPAGE 🛡 Bouclier actif"); }
            else if (reward == 7) { moneyMultUntil = Time.time + 10f; CalloutEvents.Raise("UNSTOPPABLE 💰 ×2 Argent"); }
        }

        void Update()
        {
            float ksMult = Time.time < ksShieldUntil ? 0.5f : 1f;               // web: ksShieldActive ×0.5
            float powerUpMult = powerups != null ? powerups.IncomingMult : 1f;   // web: kevlar ×0.7
            health.incomingMult = ksMult * powerUpMult;                         // composition, jamais un écrasement
            waves.bonusMoneyMult = Time.time < moneyMultUntil ? 2f : 1f;
        }
    }
}
