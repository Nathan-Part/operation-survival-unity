using Game.Data;
using Game.Data.Rules;
using Game.Data.Save;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class ShopRulesTests
    {
        // ---------- EquipWeapon ----------

        [Test]
        public void EquipWeapon_Owned_Selects()
        {
            var save = new SaveData();
            Assert.IsTrue(ShopRules.EquipWeapon(save, "default"));
            Assert.AreEqual("default", save.selectedWeapon);
        }

        [Test]
        public void EquipWeapon_NotOwned_Rejected()
        {
            var save = new SaveData();
            string before = save.selectedWeapon;
            Assert.IsFalse(ShopRules.EquipWeapon(save, "rocket-launcher"));
            Assert.AreEqual(before, save.selectedWeapon);
        }

        // ---------- SelectCharacter ----------

        [Test]
        public void SelectCharacter_Owned_Selects()
        {
            var save = new SaveData();
            Assert.IsTrue(ShopRules.SelectCharacter(save, "perso:blue-soldier"));
            Assert.AreEqual("perso:blue-soldier", save.selectedCharacterId);
        }

        [Test]
        public void SelectCharacter_NotOwned_Rejected()
        {
            var save = new SaveData();
            Assert.IsFalse(ShopRules.SelectCharacter(save, "perso:unowned"));
        }

        // ---------- Upgrades d'arme par stat (point 3 feedback n°3, parité web) ----------

        [Test]
        public void BuyWeaponUpgrade_TracksEachStatIndependently()
        {
            var save = new SaveData { money = 10000 };
            save.ownedWeapons.Add("ak");
            Assert.IsTrue(ShopRules.BuyWeaponUpgrade(save, "ak", WeaponStatKeys.Damage, 300, 5));
            Assert.IsTrue(ShopRules.BuyWeaponUpgrade(save, "ak", WeaponStatKeys.Damage, 300, 5));
            Assert.IsTrue(ShopRules.BuyWeaponUpgrade(save, "ak", WeaponStatKeys.Recoil, 200, 5));
            Assert.AreEqual(2, ShopRules.WeaponUpgradeLevel(save, "ak", WeaponStatKeys.Damage));
            Assert.AreEqual(1, ShopRules.WeaponUpgradeLevel(save, "ak", WeaponStatKeys.Recoil));
            Assert.AreEqual(0, ShopRules.WeaponUpgradeLevel(save, "ak", WeaponStatKeys.FireRate));
            Assert.AreEqual(3, ShopRules.WeaponUpgradeTotal(save, "ak"));
            // Débits : 300 (dmg N1) + 600 (dmg N2) + 200 (recul N1) = 1100
            Assert.AreEqual(10000 - 1100, save.money);
        }

        [Test]
        public void BuyWeaponUpgrade_RefusesBeyondMaxAndWithoutMoneyOrOwnership()
        {
            var save = new SaveData { money = 100000 };
            save.ownedWeapons.Add("ak");
            for (int i = 0; i < 5; i++) ShopRules.BuyWeaponUpgrade(save, "ak", WeaponStatKeys.Mag, 250, 5);
            Assert.IsFalse(ShopRules.BuyWeaponUpgrade(save, "ak", WeaponStatKeys.Mag, 250, 5)); // max atteint
            var poor = new SaveData { money = 100 };
            poor.ownedWeapons.Add("ak");
            Assert.IsFalse(ShopRules.BuyWeaponUpgrade(poor, "ak", WeaponStatKeys.Damage, 300, 5)); // pas assez
            var rich = new SaveData { money = 100000 };
            Assert.IsFalse(ShopRules.BuyWeaponUpgrade(rich, "smg", WeaponStatKeys.Damage, 300, 5)); // non possédée
        }

        [Test]
        public void WeaponUpgradeLevelsOf_MapsAllFourStats()
        {
            var save = new SaveData { money = 100000 };
            save.ownedWeapons.Add("ak");
            ShopRules.BuyWeaponUpgrade(save, "ak", WeaponStatKeys.FireRate, 300, 5);
            var levels = ShopRules.WeaponUpgradeLevelsOf(save, "ak");
            Assert.AreEqual(0, levels.damage);
            Assert.AreEqual(1, levels.fireRate);
            Assert.AreEqual(0, levels.recoil);
            Assert.AreEqual(0, levels.mag);
        }

        // ---------- BuyAccessory ----------

        [Test]
        public void BuyAccessory_Nominal_DebitsAndOwns()
        {
            var save = new SaveData { money = 100 };
            Assert.IsTrue(ShopRules.BuyAccessory(save, "helm:red", 50));
            Assert.AreEqual(50, save.money);
            CollectionAssert.Contains(save.ownedAccessories, "helm:red");
        }

        [Test]
        public void BuyAccessory_InsufficientFunds_Rejected()
        {
            var save = new SaveData { money = 10 };
            Assert.IsFalse(ShopRules.BuyAccessory(save, "helm:red", 50));
            CollectionAssert.DoesNotContain(save.ownedAccessories, "helm:red");
        }

        [Test]
        public void BuyAccessory_AlreadyOwned_Rejected()
        {
            var save = new SaveData { money = 1000 };
            Assert.IsFalse(ShopRules.BuyAccessory(save, "helm:none", 10)); // possédé par défaut
        }

        // ---------- EquipAccessory ----------

        [Test]
        public void EquipAccessory_Owned_Equips()
        {
            var save = new SaveData();
            Assert.IsTrue(ShopRules.EquipAccessory(save, "helm:none", AccessoryType.Helmet));
            Assert.AreEqual("helm:none", save.equipped["helmet"]);
        }

        [Test]
        public void EquipAccessory_NotOwned_Rejected()
        {
            var save = new SaveData();
            Assert.IsFalse(ShopRules.EquipAccessory(save, "helm:red", AccessoryType.Helmet));
        }

        // ---------- BuyBoost ----------

        [Test]
        public void BuyBoost_Nominal_DebitsAndOwns()
        {
            var save = new SaveData { money = 100 };
            Assert.IsTrue(ShopRules.BuyBoost(save, "boost:vest1", 50));
            Assert.AreEqual(50, save.money);
            CollectionAssert.Contains(save.ownedBoosts, "boost:vest1");
        }

        [Test]
        public void BuyBoost_InsufficientFunds_Rejected()
        {
            var save = new SaveData { money = 10 };
            Assert.IsFalse(ShopRules.BuyBoost(save, "boost:vest1", 50));
        }

        [Test]
        public void BuyBoost_AlreadyOwned_Rejected()
        {
            var save = new SaveData { money = 1000 };
            save.ownedBoosts.Add("boost:vest1");
            Assert.IsFalse(ShopRules.BuyBoost(save, "boost:vest1", 10));
        }

        // ---------- ToggleBoost ----------

        [Test]
        public void ToggleBoost_Owned_Equips()
        {
            var save = new SaveData();
            save.ownedBoosts.Add("boost:vest1");
            Assert.IsTrue(ShopRules.ToggleBoost(save, "boost:vest1", BoostSlot.Vest));
            Assert.AreEqual("boost:vest1", save.equippedBoosts["vest"]);
        }

        [Test]
        public void ToggleBoost_NotOwned_Rejected()
        {
            var save = new SaveData();
            Assert.IsFalse(ShopRules.ToggleBoost(save, "boost:vest1", BoostSlot.Vest));
        }

        [Test]
        public void ToggleBoost_ReclickSameId_Unequips()
        {
            var save = new SaveData();
            save.ownedBoosts.Add("boost:vest1");
            ShopRules.ToggleBoost(save, "boost:vest1", BoostSlot.Vest);
            Assert.IsTrue(ShopRules.ToggleBoost(save, "boost:vest1", BoostSlot.Vest));
            Assert.IsFalse(save.equippedBoosts.ContainsKey("vest"));
        }

        [Test]
        public void ToggleBoost_DifferentBoostSameSlot_Replaces()
        {
            var save = new SaveData();
            save.ownedBoosts.Add("boost:vest1");
            save.ownedBoosts.Add("boost:vest2");
            ShopRules.ToggleBoost(save, "boost:vest1", BoostSlot.Vest);
            Assert.IsTrue(ShopRules.ToggleBoost(save, "boost:vest2", BoostSlot.Vest));
            Assert.AreEqual("boost:vest2", save.equippedBoosts["vest"]);
            Assert.AreEqual(1, save.equippedBoosts.Count);
        }

        // ---------- BuyDance ----------

        [Test]
        public void BuyDance_Nominal_DebitsAndOwns()
        {
            var save = new SaveData { money = 100 };
            Assert.IsTrue(ShopRules.BuyDance(save, "dance:new", 50));
            Assert.AreEqual(50, save.money);
            CollectionAssert.Contains(save.ownedDances, "dance:new");
        }

        [Test]
        public void BuyDance_InsufficientFunds_Rejected()
        {
            var save = new SaveData { money = 10 };
            Assert.IsFalse(ShopRules.BuyDance(save, "dance:new", 50));
        }

        [Test]
        public void BuyDance_AlreadyOwned_Rejected()
        {
            var save = new SaveData { money = 1000 };
            Assert.IsFalse(ShopRules.BuyDance(save, "dance:boom", 10)); // possédée par défaut
        }

        // ---------- ToggleDanceSelection ----------
        // web: src/game/main.js toggleDance() — retire si présente, sinon push + shift (retire la plus ancienne) si > max.

        [Test]
        public void ToggleDanceSelection_NotOwned_Rejected()
        {
            var save = new SaveData();
            Assert.IsFalse(ShopRules.ToggleDanceSelection(save, "dance:unowned"));
        }

        [Test]
        public void ToggleDanceSelection_RoundTrip_RemovesThenReAdds()
        {
            var save = new SaveData(); // ownedDances = selectedDances = [all_night, boom, groove]
            Assert.IsTrue(ShopRules.ToggleDanceSelection(save, "dance:boom"));
            CollectionAssert.DoesNotContain(save.selectedDances, "dance:boom");
            Assert.AreEqual(2, save.selectedDances.Count);

            Assert.IsTrue(ShopRules.ToggleDanceSelection(save, "dance:boom"));
            CollectionAssert.Contains(save.selectedDances, "dance:boom");
            Assert.AreEqual(3, save.selectedDances.Count);
        }

        [Test]
        public void ToggleDanceSelection_MaxReached_ShiftsOutOldest()
        {
            var save = new SaveData(); // selected = [all_night, boom, groove], déjà au max (3)
            save.ownedDances.Add("dance:fourth");
            Assert.IsTrue(ShopRules.ToggleDanceSelection(save, "dance:fourth"));
            Assert.AreEqual(3, save.selectedDances.Count);
            CollectionAssert.DoesNotContain(save.selectedDances, "dance:all_night"); // la plus ancienne est sortie
            CollectionAssert.Contains(save.selectedDances, "dance:fourth");
        }

        // ---------- BuyConsumable ----------

        [Test]
        public void BuyConsumable_ExistingKey_IncrementsAndDebits()
        {
            var save = new SaveData { money = 100 };
            int before = save.consumables["frag"];
            Assert.IsTrue(ShopRules.BuyConsumable(save, "frag", 20));
            Assert.AreEqual(before + 1, save.consumables["frag"]);
            Assert.AreEqual(80, save.money);
        }

        [Test]
        public void BuyConsumable_InsufficientFunds_Rejected()
        {
            var save = new SaveData { money = 5 };
            int before = save.consumables["frag"];
            Assert.IsFalse(ShopRules.BuyConsumable(save, "frag", 20));
            Assert.AreEqual(before, save.consumables["frag"]);
        }

        [Test]
        public void BuyConsumable_AbsentKey_CreatesAtZeroThenIncrements()
        {
            var save = new SaveData { money = 100 };
            Assert.IsFalse(save.consumables.ContainsKey("newkey"));
            Assert.IsTrue(ShopRules.BuyConsumable(save, "newkey", 20));
            Assert.AreEqual(1, save.consumables["newkey"]);
        }

        // ---------- BuyPlayerUpgrade / PlayerUpgradeLevel / NextPlayerUpgradeCost ----------

        [Test]
        public void PlayerUpgradeLevel_Default_IsZero()
        {
            var save = new SaveData();
            Assert.AreEqual(0, ShopRules.PlayerUpgradeLevel(save, "health"));
        }

        [Test]
        public void NextPlayerUpgradeCost_ScalesWithLevel()
        {
            var save = new SaveData();
            Assert.AreEqual(300, ShopRules.NextPlayerUpgradeCost(save, "health", 300));
            save.playerUpgrades["health"] = 2;
            Assert.AreEqual(900, ShopRules.NextPlayerUpgradeCost(save, "health", 300));
        }

        [Test]
        public void BuyPlayerUpgrade_Nominal_DebitsAndLevelsUp()
        {
            var save = new SaveData { money = 1000 };
            Assert.IsTrue(ShopRules.BuyPlayerUpgrade(save, "health", 300, 5));
            Assert.AreEqual(700, save.money);
            Assert.AreEqual(1, save.playerUpgrades["health"]);
        }

        [Test]
        public void BuyPlayerUpgrade_InsufficientFunds_Rejected()
        {
            var save = new SaveData { money = 10 };
            Assert.IsFalse(ShopRules.BuyPlayerUpgrade(save, "health", 300, 5));
        }

        [Test]
        public void BuyPlayerUpgrade_MaxLevelReached_Rejected()
        {
            var save = new SaveData { money = 100000 };
            save.playerUpgrades["health"] = 5;
            Assert.IsFalse(ShopRules.BuyPlayerUpgrade(save, "health", 300, 5));
        }

        // ---------- ToggleLoadout ----------
        // web: src/game/main.js toggleLoadoutBonus() — retire si présent, sinon push + shift (retire le plus ancien) si > max.

        [Test]
        public void ToggleLoadout_RoundTrip_RemovesThenReAdds()
        {
            var save = new SaveData(); // selectedLoadout = [turret, mine, airstrike]
            Assert.IsTrue(ShopRules.ToggleLoadout(save, "mine"));
            CollectionAssert.DoesNotContain(save.selectedLoadout, "mine");
            Assert.AreEqual(2, save.selectedLoadout.Count);

            Assert.IsTrue(ShopRules.ToggleLoadout(save, "mine"));
            CollectionAssert.Contains(save.selectedLoadout, "mine");
            Assert.AreEqual(3, save.selectedLoadout.Count);
        }

        [Test]
        public void ToggleLoadout_MaxReached_ShiftsOutOldest()
        {
            var save = new SaveData(); // déjà au max (3) : [turret, mine, airstrike]
            Assert.IsTrue(ShopRules.ToggleLoadout(save, "carepackage"));
            Assert.AreEqual(3, save.selectedLoadout.Count);
            CollectionAssert.DoesNotContain(save.selectedLoadout, "turret"); // la plus ancienne est sortie
            CollectionAssert.Contains(save.selectedLoadout, "carepackage");
        }
    }
}
