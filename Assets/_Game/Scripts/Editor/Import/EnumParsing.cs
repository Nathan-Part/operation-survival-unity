using System;

namespace Game.Data.Editor.Import
{
    public static class EnumParsing
    {
        public static Rarity ParseRarity(string value) => value switch
        {
            "commun" => Rarity.Common,
            "rare" => Rarity.Rare,
            "epique" => Rarity.Epic,
            "legendaire" => Rarity.Legendary,
            _ => throw new ArgumentException($"Unknown rarity '{value}'")
        };

        public static WeaponCategory ParseWeaponCategory(string value) => value switch
        {
            "assault" => WeaponCategory.Assault,
            "smg" => WeaponCategory.Smg,
            "sniper" => WeaponCategory.Sniper,
            "shotgun" => WeaponCategory.Shotgun,
            "pistol" => WeaponCategory.Pistol,
            "lmg" => WeaponCategory.Lmg,
            "melee" => WeaponCategory.Melee,
            _ => throw new ArgumentException($"Unknown weapon category '{value}'")
        };

        public static ArmorType ParseArmor(string value) => value switch
        {
            null => ArmorType.None,
            "torso" => ArmorType.Torso,
            "full" => ArmorType.Full,
            _ => throw new ArgumentException($"Unknown armor type '{value}'")
        };

        public static PvpPolicy ParsePvpPolicy(string value) => value switch
        {
            "allowed" => PvpPolicy.Allowed,
            "rebalance" => PvpPolicy.Rebalance,
            "exclude" => PvpPolicy.Exclude,
            _ => throw new ArgumentException($"Unknown pvp policy '{value}'")
        };

        public static BoostSlot ParseBoostSlot(string value) => value switch
        {
            "vest" => BoostSlot.Vest,
            "boots" => BoostSlot.Boots,
            "medal" => BoostSlot.Medal,
            _ => throw new ArgumentException($"Unknown boost slot '{value}'")
        };

        public static BoostStat ParseBoostStat(string value) => value switch
        {
            "health" => BoostStat.Health,
            "regen" => BoostStat.Regen,
            "speed" => BoostStat.Speed,
            _ => throw new ArgumentException($"Unknown boost stat '{value}'")
        };
    }
}
