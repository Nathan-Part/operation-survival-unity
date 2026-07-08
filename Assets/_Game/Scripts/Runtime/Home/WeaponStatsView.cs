using Game.Data;
using Game.Data.Rules;
using UnityEngine;

namespace Game.Runtime
{
    /// 5 barres de stats de la fiche d'arme boutique (parité web `renderShop`, branche armes à feu
    /// — main.js:1461-1500, formules exactes lignes 1483-1487) :
    ///   const dmg = Math.round(60 * s.dmgMult), rpm = Math.round(60 / s.cooldown);   // l.1483
    ///   statBar('Dégâts',  dmg/200*100, dmg)                                          // l.1485
    ///   statBar('Cadence', rpm/900*100, rpm)                                          // l.1485
    ///   statBar('Chargeur', s.mag/55*100, s.mag)                                      // l.1486
    ///   statBar('Stabilité', (3-s.recoil)/3*100, (3-s.recoil).toFixed(1))             // l.1486
    ///   statBar('Portée', (s.range??80)/200*100, s.range+'m')                         // l.1487
    /// `s` = weaponStats(id) (main.js:1158-1163) : stats de base + effet des niveaux d'amélioration
    /// (UPGRADE_SPECS.apply, main.js:1140-1143) — repris ici via `UpgradeFormulas` (Data/Rules),
    /// déjà utilisé par WeaponController pour le gameplay réel, plutôt que de redupliquer les
    /// coefficients (1+0.15l, 1-0.08l, 1-0.12l, 1+0.2l arrondi) une deuxième fois.
    ///
    /// Simplification assumée (documentée dans le rapport de tâche 11) : le web applique un jeu de
    /// SEULEMENT 3 barres différentes aux armes de mêlée (Dégâts/Vitesse/Portée, main.js:1477-1481,
    /// basé sur PLAYER_MELEE_DAMAGE). Ce composant n'implémente que les 5 méthodes du contrat de la
    /// tâche (branche armes à feu) et les applique uniformément aux 15 armes, mêlée comprise — un
    /// couteau affichera donc une barre "Cadence"/"Chargeur" au lieu des 3 barres mêlée du web.
    ///
    /// Autre limite héritée (pas introduite ici) : le reste du projet (ShopRules.WeaponUpgradeLevel,
    /// WeaponController.cs:64-67) ne suit qu'UN SEUL niveau d'amélioration par arme (partagé par les
    /// 4 stats), alors que le web suit 4 niveaux indépendants (dmgLevel/fireRateLevel/magLevel/
    /// recoilLevel séparés). Chaque méthode ci-dessous accepte bien son propre niveau en paramètre
    /// (donc reste correcte et testable indépendamment pour le cas général) ; c'est l'appelant
    /// (ShopWeaponsTab) qui, en pratique, passe la même valeur partagée aux quatre — cohérent avec
    /// le reste du jeu, pas une régression de cette tâche.
    public static class WeaponStatsView
    {
        // web: dmg = round(60 * dmgMult) (l.1483) ; barre = dmg/200 (l.1485)
        public static float Damage(WeaponData w, int dmgLevel) => Clamp01(DamageRaw(w, dmgLevel) / 200f);

        public static int DamageRaw(WeaponData w, int dmgLevel) =>
            Mathf.RoundToInt(60f * UpgradeFormulas.WeaponDamageMult(w.dmgMult, dmgLevel));

        // web: rpm = round(60 / cooldown) (l.1483) ; barre = rpm/900 (l.1485)
        public static float FireRate(WeaponData w, int fireRateLevel) => Clamp01(FireRateRaw(w, fireRateLevel) / 900f);

        public static int FireRateRaw(WeaponData w, int fireRateLevel) =>
            Mathf.RoundToInt(60f / UpgradeFormulas.WeaponCooldown(w.cooldown, fireRateLevel));

        // web: barre = mag/55 (l.1486)
        public static float MagSize(WeaponData w, int magLevel) => Clamp01(MagSizeRaw(w, magLevel) / 55f);

        public static int MagSizeRaw(WeaponData w, int magLevel) => UpgradeFormulas.WeaponMag(w.mag, magLevel);

        // web: barre = (3-recoil)/3 (l.1486)
        public static float Stability(WeaponData w, int recoilLevel) => Clamp01(StabilityRaw(w, recoilLevel) / 3f);

        public static float StabilityRaw(WeaponData w, int recoilLevel) => 3f - UpgradeFormulas.WeaponRecoil(w.recoil, recoilLevel);

        // web: barre = (range ?? 80)/200 (l.1487) — la portée n'a pas de niveau d'amélioration
        // (absente de UPGRADE_SPECS/UpgradeConfig). w.range vaut 0 quand le JSON source n'a pas de
        // clé "range" (armes de mêlée) : équivalent du `??` JS (déclenché sur undefined, jamais sur
        // une vraie portée positive).
        public static float Range(WeaponData w) => Clamp01(RangeRaw(w) / 200f);

        public static float RangeRaw(WeaponData w) => w.range > 0f ? w.range : 80f;

        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
