using System.Collections.Generic;
using Game.Data.Save;

namespace Game.Data.Rules
{
    /// Logique d'achat (boutique web) : armes, persos, upgrades d'arme, accessoires,
    /// boosts, danses, consommables, upgrades joueur, loadout.
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

        public static bool EquipWeapon(SaveData save, string weaponId)
        {
            if (save == null || !save.ownedWeapons.Contains(weaponId)) return false;
            save.selectedWeapon = weaponId;
            return true;
        }

        public static bool BuyCharacter(SaveData save, string characterId, int price)
        {
            if (!CanBuy(save, save.ownedCharacters, characterId, price)) return false;
            save.money -= price;
            save.ownedCharacters.Add(characterId);
            return true;
        }

        public static bool SelectCharacter(SaveData save, string characterId)
        {
            if (save == null || !save.ownedCharacters.Contains(characterId)) return false;
            save.selectedCharacterId = characterId;
            return true;
        }

        /// Niveau d'amélioration de CETTE stat de CETTE arme (web upgradeLevel, main.js:1145 —
        /// 4 progressions indépendantes par arme, point 3 feedback n°3).
        public static int WeaponUpgradeLevel(SaveData save, string weaponId, string statKey) =>
            save.weaponUpgrades.TryGetValue(weaponId, out var stats) && stats != null &&
            stats.TryGetValue(statKey, out int level) ? level : 0;

        /// Somme des 4 stats — le rating « ★ x/20 » du web (main.js:1452 : reduce sur UPGRADE_SPECS).
        public static int WeaponUpgradeTotal(SaveData save, string weaponId)
        {
            int total = 0;
            foreach (var key in WeaponStatKeys.All) total += WeaponUpgradeLevel(save, weaponId, key);
            return total;
        }

        /// Coût du prochain niveau de CETTE stat (formule web : baseCost × (niveau+1)).
        public static int NextWeaponUpgradeCost(SaveData save, string weaponId, string statKey, int baseCost) =>
            UpgradeFormulas.Cost(baseCost, WeaponUpgradeLevel(save, weaponId, statKey));

        public static bool BuyWeaponUpgrade(SaveData save, string weaponId, string statKey, int baseCost, int maxLevel)
        {
            int level = WeaponUpgradeLevel(save, weaponId, statKey);
            if (level >= maxLevel) return false;
            int cost = UpgradeFormulas.Cost(baseCost, level);
            if (save.money < cost || !save.ownedWeapons.Contains(weaponId)) return false;
            save.money -= cost;
            if (!save.weaponUpgrades.TryGetValue(weaponId, out var stats) || stats == null)
                save.weaponUpgrades[weaponId] = stats = new Dictionary<string, int>();
            stats[statKey] = level + 1;
            return true;
        }

        public static WeaponUpgradeLevels WeaponUpgradeLevelsOf(SaveData save, string weaponId) => new WeaponUpgradeLevels
        {
            damage = WeaponUpgradeLevel(save, weaponId, WeaponStatKeys.Damage),
            fireRate = WeaponUpgradeLevel(save, weaponId, WeaponStatKeys.FireRate),
            recoil = WeaponUpgradeLevel(save, weaponId, WeaponStatKeys.Recoil),
            mag = WeaponUpgradeLevel(save, weaponId, WeaponStatKeys.Mag),
        };

        public static bool BuyAccessory(SaveData save, string id, int price)
        {
            if (!CanBuy(save, save.ownedAccessories, id, price)) return false;
            save.money -= price;
            save.ownedAccessories.Add(id);
            return true;
        }

        public static bool EquipAccessory(SaveData save, string id, AccessoryType type)
        {
            if (save == null || !save.ownedAccessories.Contains(id)) return false;
            save.equipped[type == AccessoryType.Helmet ? "helmet" : "costume"] = id;
            return true;
        }

        public static bool BuyBoost(SaveData save, string id, int price)
        {
            if (!CanBuy(save, save.ownedBoosts, id, price)) return false;
            save.money -= price;
            save.ownedBoosts.Add(id);
            return true;
        }

        /// Équipe le boost sur son slot ; re-cliquer sur le boost déjà équipé le retire.
        /// Équiper un autre boost sur un slot déjà occupé remplace l'ancien (jamais additif).
        public static bool ToggleBoost(SaveData save, string id, BoostSlot slot)
        {
            if (save == null || !save.ownedBoosts.Contains(id)) return false;
            string slotKey = slot.ToString().ToLowerInvariant();
            if (save.equippedBoosts.TryGetValue(slotKey, out string current) && current == id)
                save.equippedBoosts.Remove(slotKey);
            else
                save.equippedBoosts[slotKey] = id;
            return true;
        }

        public static bool BuyDance(SaveData save, string id, int price)
        {
            if (!CanBuy(save, save.ownedDances, id, price)) return false;
            save.money -= price;
            save.ownedDances.Add(id);
            return true;
        }

        /// web (main.js toggleDance) : retire si sélectionnée ; sinon l'ajoute et, si ça
        /// dépasse `max`, retire la plus ancienne (FIFO) — jamais de refus par plafond.
        public static bool ToggleDanceSelection(SaveData save, string id, int max = 3)
        {
            if (save == null || !save.ownedDances.Contains(id)) return false;
            var arr = save.selectedDances;
            int i = arr.IndexOf(id);
            if (i >= 0) arr.RemoveAt(i);
            else
            {
                arr.Add(id);
                if (arr.Count > max) arr.RemoveAt(0);
            }
            return true;
        }

        public static bool BuyConsumable(SaveData save, string key, int price)
        {
            if (save == null || save.money < price) return false;
            if (!save.consumables.ContainsKey(key)) save.consumables[key] = 0;
            save.money -= price;
            save.consumables[key] += 1;
            return true;
        }

        public static int PlayerUpgradeLevel(SaveData save, string stat) =>
            save.playerUpgrades.TryGetValue(stat, out int level) ? level : 0;

        /// Coût du prochain niveau (miroir de NextWeaponUpgradeCost).
        public static int NextPlayerUpgradeCost(SaveData save, string stat, int baseCost) =>
            UpgradeFormulas.Cost(baseCost, PlayerUpgradeLevel(save, stat));

        public static bool BuyPlayerUpgrade(SaveData save, string stat, int baseCost, int maxLevel)
        {
            int level = PlayerUpgradeLevel(save, stat);
            if (level >= maxLevel) return false;
            int cost = UpgradeFormulas.Cost(baseCost, level);
            if (save.money < cost) return false;
            save.money -= cost;
            save.playerUpgrades[stat] = level + 1;
            return true;
        }

        /// web (main.js toggleLoadoutBonus) : retire si présent ; sinon l'ajoute et, si ça
        /// dépasse `max`, retire le plus ancien (FIFO) — jamais de refus par plafond.
        public static bool ToggleLoadout(SaveData save, string key, int max = 3)
        {
            if (save == null) return false;
            var arr = save.selectedLoadout;
            int i = arr.IndexOf(key);
            if (i >= 0) arr.RemoveAt(i);
            else
            {
                arr.Add(key);
                if (arr.Count > max) arr.RemoveAt(0);
            }
            return true;
        }
    }
}
