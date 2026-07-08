using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    /// Applique des dégâts d'explosion (falloff linéaire, web: SpecialRules.ExplosionDamage)
    /// à tous les ennemis vivants dans un rayon. Renvoie le nombre de kills.
    public static class AreaDamage
    {
        public static int Apply(IReadOnlyList<EnemyHealth> enemies, Vector3 center, float radius, float damage)
        {
            int killed = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null || e.IsDead) continue;
                int dmg = SpecialRules.ExplosionDamage(Vector3.Distance(center, e.transform.position), radius, damage);
                if (dmg <= 0) continue;
                e.ApplyDamage(dmg);
                if (e.IsDead) killed++;
            }
            return killed;
        }
    }
}
