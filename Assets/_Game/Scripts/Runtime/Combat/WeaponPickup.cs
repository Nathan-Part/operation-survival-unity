using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    /// Ramassage d'arme sur cadavre (G, web #tranche23) : scanne les cadavres non pillés dans
    /// PICKUP_R et ré-équipe l'arme du run (chargeur plein, niveau d'upgrade 0 — loot run-based,
    /// n'altère jamais la save, cf. spec web « matchWeaponId »).
    public class WeaponPickup : MonoBehaviour
    {
        const float PickupR = 2.5f; // web: PICKUP_R=2.5

        Transform player;
        WeaponController weapon;
        GameDatabase db;

        public void Init(Transform player, WeaponController weapon, GameDatabase db)
        {
            this.player = player;
            this.weapon = weapon;
            this.db = db;
        }

        /// Cadavre pillable le plus proche à ≤ PICKUP_R, ou null si aucun.
        EnemyController FindNearest()
        {
            EnemyController best = null;
            float bestDist = PickupR;
            foreach (var c in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
            {
                if (c == null || c.Looted || string.IsNullOrEmpty(c.DropWeaponId)) continue;
                float d = Vector3.Distance(player.position, c.transform.position);
                if (d <= bestDist) { bestDist = d; best = c; }
            }
            return best;
        }

        public void TryPickup()
        {
            if (player == null || weapon == null || db == null) return;
            var best = FindNearest();
            if (best == null) return;

            WeaponData picked = null;
            foreach (var w in db.weapons)
                if (w != null && w.id == best.DropWeaponId) { picked = w; break; }
            if (picked == null) return;

            weapon.Init(picked); // upgradeLevel 0 : loot run-based, jamais la fiche d'upgrade de la save
            best.MarkLooted();
            CalloutEvents.Raise($"ARME : {picked.displayName}");
        }
    }
}
