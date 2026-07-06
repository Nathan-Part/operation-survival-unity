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

        public static int Damage(HitZone zone, HitZoneSettings zones, float weaponDmgMult, ArmorType armor, float armorDamageMult)
        {
            int zoneDamage = zone == HitZone.Head ? zones.headDamage
                : zone == HitZone.Legs ? zones.legsDamage
                : zones.torsoDamage;
            bool isProtected = armor == ArmorType.Full || (armor == ArmorType.Torso && zone == HitZone.Torso);
            double damage = zoneDamage * weaponDmgMult * (isProtected ? armorDamageMult : 1f);
            return (int)Math.Round(damage, MidpointRounding.AwayFromZero);
        }
    }
}
