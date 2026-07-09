using System;
using Game.Data;

namespace Game.Runtime
{
    public enum HitZone { Head, Torso, Legs }

    /// Spec web (enemies.json -> hitZones) : zone par hauteur d'impact relative aux pieds,
    /// normalisée par le scale du modèle (référence 1.8 m).
    public static class HitZoneResolver
    {
        public static HitZone Resolve(float yLocal, float scale, HitZoneSettings zones)
        {
            float y = scale > 0f ? yLocal / scale : yLocal;
            if (y >= zones.headAboveY) return HitZone.Head;
            if (y < zones.legsBelowY) return HitZone.Legs;
            return HitZone.Torso;
        }

        // Valeurs reelles web (main.js:2322-2331, #114 Armor system) — playtest Phase 5.1 (2026-07-09) :
        // remplace l'ancien multiplicateur flat 0.5 (TODO(spec-web) leve) par 2 constantes distinctes.
        // Torse (tank/boss) : reduction UNIQUEMENT si la zone touchee est le torse (un headshot passe
        // toujours a plein degats). Integrale (juggernaut) : reduction sur TOUTE zone.
        public const float ArmorTorsoMult = 0.7f;
        public const float ArmorFullMult = 0.6f;

        public static int Damage(HitZone zone, HitZoneSettings zones, float weaponDmgMult, ArmorType armor)
        {
            int zoneDamage = zone == HitZone.Head ? zones.headDamage
                : zone == HitZone.Legs ? zones.legsDamage
                : zones.torsoDamage;
            float armorMult = armor == ArmorType.Full ? ArmorFullMult
                : (armor == ArmorType.Torso && zone == HitZone.Torso) ? ArmorTorsoMult
                : 1f;
            double damage = zoneDamage * weaponDmgMult * armorMult;
            return (int)Math.Round(damage, MidpointRounding.AwayFromZero);
        }
    }
}
