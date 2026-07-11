namespace Game.Data.Rules
{
    /// Clés du schéma de save web `weaponUpgrades[armeId][stat]` (save.js / UPGRADE_SPECS,
    /// main.js:1139-1144). Identifiants de schéma — les coûts/niveaux, eux, viennent de
    /// GameDatabase.upgradeConfig (unity-spec/items.json).
    public static class WeaponStatKeys
    {
        public const string Damage = "damage";
        public const string FireRate = "fireRate";
        public const string Recoil = "recoil";
        public const string Mag = "mag";

        public static readonly string[] All = { Damage, FireRate, Recoil, Mag };
    }

    /// Niveaux d'amélioration d'une arme, une valeur par stat (parité web weaponStats,
    /// main.js:1158-1162). `default` = arme sans amélioration (loot en partie, WeaponPickup).
    public struct WeaponUpgradeLevels
    {
        public int damage;
        public int fireRate;
        public int recoil;
        public int mag;
    }
}
