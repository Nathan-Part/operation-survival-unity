using Game.Data.Rules;
using Game.Data.Save;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class SaveMigrationTests
    {
        [Test]
        public void FromJson_EmptyOrNull_GivesWebDefaults()
        {
            var save = SaveMigration.FromJson(null);
            Assert.AreEqual(1, save.v);
            Assert.AreEqual(0, save.money);
            CollectionAssert.Contains(save.ownedWeapons, "default");
            CollectionAssert.Contains(save.ownedWeapons, "knife");         // knife gratuit (#50)
            CollectionAssert.Contains(save.ownedCharacters, "perso:blue-soldier");
            Assert.AreEqual(12, save.consumables["frag"]);
            Assert.AreEqual(3, save.consumables["medkit"]);
            Assert.AreEqual("third", save.settings.defaultView);
            Assert.AreEqual(0.3f, save.settings.volume, 1e-4f);
            Assert.AreEqual(3, save.selectedLoadout.Count);
            Assert.AreEqual(0, save.prestige);
        }

        [Test]
        public void FromJson_CorruptJson_GivesDefaults()
        {
            Assert.AreEqual(0, SaveMigration.FromJson("{pas du json").money);
            Assert.AreEqual(0, SaveMigration.FromJson("42").money);        // pas un objet
        }

        [Test]
        public void FromJson_PartialSave_MergesOnDefaults()
        {
            var save = SaveMigration.FromJson("{\"money\":500,\"xp\":1200,\"settings\":{\"volume\":0.8}}");
            Assert.AreEqual(500, save.money);
            Assert.AreEqual(1200, save.xp);
            Assert.AreEqual(0.8f, save.settings.volume, 1e-4f);
            Assert.AreEqual("third", save.settings.defaultView);           // défaut conservé
            Assert.AreEqual(3, save.consumables["medkit"]);                // migration #178
        }

        [Test]
        public void FromJson_OldSaveWithoutKnife_GetsKnifeBack()
        {
            var save = SaveMigration.FromJson("{\"ownedWeapons\":[\"default\",\"raptor\"]}");
            CollectionAssert.Contains(save.ownedWeapons, "knife");
            CollectionAssert.Contains(save.ownedWeapons, "raptor");
        }

        [Test]
        public void RoundTrip_PreservesData()
        {
            var save = new SaveData { money = 777, bestWave = 9, prestige = 2 };
            save.weaponUpgrades["raptor"] = new System.Collections.Generic.Dictionary<string, int>
            {
                { WeaponStatKeys.Damage, 3 }
            };
            var back = SaveMigration.FromJson(SaveMigration.ToJson(save));
            Assert.AreEqual(777, back.money);
            Assert.AreEqual(9, back.bestWave);
            Assert.AreEqual(2, back.prestige);
            Assert.AreEqual(3, ShopRules.WeaponUpgradeLevel(back, "raptor", WeaponStatKeys.Damage));
        }

        [Test]
        public void FromJson_MigratesLegacySingleLevelWeaponUpgradesToPerStat()
        {
            // Ancien schéma Unity (niveau unique par arme, appliqué aux 4 stats en jeu) :
            // fidélité = reporter ce niveau sur chacune des 4 stats.
            var save = SaveMigration.FromJson("{\"weaponUpgrades\":{\"ak\":3}}");
            foreach (var key in WeaponStatKeys.All)
                Assert.AreEqual(3, ShopRules.WeaponUpgradeLevel(save, "ak", key), key);
        }

        [Test]
        public void FromJson_KeepsWebPerStatWeaponUpgradesAsIs()
        {
            var save = SaveMigration.FromJson("{\"weaponUpgrades\":{\"ak\":{\"damage\":2,\"mag\":1}}}");
            Assert.AreEqual(2, ShopRules.WeaponUpgradeLevel(save, "ak", WeaponStatKeys.Damage));
            Assert.AreEqual(1, ShopRules.WeaponUpgradeLevel(save, "ak", WeaponStatKeys.Mag));
            Assert.AreEqual(0, ShopRules.WeaponUpgradeLevel(save, "ak", WeaponStatKeys.FireRate));
        }

        // ---- ProgressionRules ----

        [Test]
        public void Progression_LevelsMatchWebFormula()
        {
            Assert.AreEqual(1, ProgressionRules.LevelFor(0));
            Assert.AreEqual(1, ProgressionRules.LevelFor(99));   // xpForLevel(1)=100
            Assert.AreEqual(2, ProgressionRules.LevelFor(100));
            Assert.AreEqual(2, ProgressionRules.LevelFor(299));  // xpForLevel(2)=300
            Assert.AreEqual(3, ProgressionRules.LevelFor(300));
            Assert.AreEqual(50, ProgressionRules.LevelFor(long.MaxValue)); // plafonné
        }

        [Test]
        public void Progression_KillXpWithHeadshotAndVip()
        {
            Assert.AreEqual(25, ProgressionRules.KillXp(false, false));
            Assert.AreEqual(50, ProgressionRules.KillXp(true, false));
            Assert.AreEqual(100, ProgressionRules.KillXp(true, true)); // ×2 VIP
        }

        // ---- ShopRules ----

        [Test]
        public void Shop_BuyWeapon_ChecksMoneyAndOwnership()
        {
            var save = new SaveData { money = 100 };
            Assert.IsFalse(ShopRules.BuyWeapon(save, "raptor", 150)); // trop cher
            Assert.IsTrue(ShopRules.BuyWeapon(save, "raptor", 80));
            Assert.AreEqual(20, save.money);
            Assert.IsFalse(ShopRules.BuyWeapon(save, "raptor", 10));  // déjà possédée
        }

        [Test]
        public void Shop_WeaponUpgrade_CostGrowsPerLevel()
        {
            var save = new SaveData { money = 1000 };
            save.ownedWeapons.Add("raptor");
            Assert.AreEqual(100, ShopRules.NextWeaponUpgradeCost(save, "raptor", WeaponStatKeys.Damage, 100)); // niveau 0 : base×1
            Assert.IsTrue(ShopRules.BuyWeaponUpgrade(save, "raptor", WeaponStatKeys.Damage, 100, 5));
            Assert.AreEqual(900, save.money);
            Assert.AreEqual(200, ShopRules.NextWeaponUpgradeCost(save, "raptor", WeaponStatKeys.Damage, 100)); // niveau 1 : base×2
            Assert.IsTrue(ShopRules.BuyWeaponUpgrade(save, "raptor", WeaponStatKeys.Damage, 100, 5));
            Assert.AreEqual(700, save.money);
            Assert.AreEqual(2, ShopRules.WeaponUpgradeLevel(save, "raptor", WeaponStatKeys.Damage));
        }

        [Test]
        public void Shop_WeaponUpgrade_RespectsMaxLevelAndOwnership()
        {
            var save = new SaveData { money = 10000 };
            Assert.IsFalse(ShopRules.BuyWeaponUpgrade(save, "raptor", WeaponStatKeys.Damage, 100, 5)); // non possédée
            save.ownedWeapons.Add("raptor");
            save.weaponUpgrades["raptor"] = new System.Collections.Generic.Dictionary<string, int>
            {
                { WeaponStatKeys.Damage, 5 }
            };
            Assert.IsFalse(ShopRules.BuyWeaponUpgrade(save, "raptor", WeaponStatKeys.Damage, 100, 5)); // niveau max
        }
    }
}
