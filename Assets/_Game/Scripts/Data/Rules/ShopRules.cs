using System.Collections.Generic;
using Game.Data.Save;

namespace Game.Data.Rules
{
    /// Logique d'achat (boutique web) : armes, persos, upgrades d'arme.
    /// Pure : mutations du SaveData uniquement, l'UI viendra en Phase 5.
    public static class ShopRules
    {
        public static bool CanBuy(SaveData save, IReadOnlyCollection<string> owned, string id, int price) =>
            save != null && !string.IsNullOrEmpty(id) && !((ICollection<string>)owned).Contains(id) && save.money >= price;

        public static bool BuyWeapon(SaveData save, string weaponId, int price)
        {
            if (!CanBuy(save, save.ownedWeapons, weaponId, price)) return false;
            save.money -= price;
            save.ownedWeapons.Add(weaponId);
            return true;
        }

        public static bool BuyCharacter(SaveData save, string characterId, int price)
        {
            if (!CanBuy(save, save.ownedCharacters, characterId, price)) return false;
            save.money -= price;
            save.ownedCharacters.Add(characterId);
            return true;
        }

        public static int WeaponUpgradeLevel(SaveData save, string weaponId) =>
            save.weaponUpgrades.TryGetValue(weaponId, out int level) ? level : 0;

        /// Coût du prochain niveau (formule importée : baseCost × (niveau+1)).
        public static int NextWeaponUpgradeCost(SaveData save, string weaponId, int baseCost) =>
            UpgradeFormulas.Cost(baseCost, WeaponUpgradeLevel(save, weaponId));

        public static bool BuyWeaponUpgrade(SaveData save, string weaponId, int baseCost, int maxLevel)
        {
            int level = WeaponUpgradeLevel(save, weaponId);
            if (level >= maxLevel) return false;
            int cost = UpgradeFormulas.Cost(baseCost, level);
            if (save.money < cost || !save.ownedWeapons.Contains(weaponId)) return false;
            save.money -= cost;
            save.weaponUpgrades[weaponId] = level + 1;
            return true;
        }
    }
}
