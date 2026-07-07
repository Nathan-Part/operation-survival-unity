using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class ItemsImporterTests
    {
        const string Fixture = "Assets/_Game/Tests/Editor/Fixtures/items.test.json";
        const string Root = "Assets/_Game/Tests/Editor/Fixtures/Generated";
        const string AccessoriesFolder = Root + "/Accessories";
        const string BoostsFolder = Root + "/Boosts";
        const string ConsumablesFolder = Root + "/Consumables";
        const string DancesFolder = Root + "/Dances";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(Root);
        }

        [Test]
        public void Import_CreatesAllFourItemCatalogs()
        {
            var result = ItemsImporter.Import(Fixture, AccessoriesFolder, BoostsFolder, ConsumablesFolder, DancesFolder, new ImportReport());

            Assert.AreEqual(3, result.Accessories.Count);
            var insectHelm = result.Accessories.Find(a => a.id == "helm:insect");
            Assert.AreEqual(AccessoryType.Helmet, insectHelm.accessoryType);
            Assert.AreEqual(1, insectHelm.bonuses.Length);
            var noneCostume = result.Accessories.Find(a => a.id == "cost:none");
            Assert.AreEqual(AccessoryType.Costume, noneCostume.accessoryType);

            Assert.AreEqual(2, result.Boosts.Count);
            var vest1 = result.Boosts.Find(b => b.id == "boost:vest1");
            Assert.AreEqual(BoostSlot.Vest, vest1.slot);
            Assert.AreEqual(BoostStat.Health, vest1.stat);
            Assert.AreEqual(25f, vest1.bonus, 0.0001f);

            Assert.AreEqual(7, result.Consumables.Count);
            var frag = result.Consumables.Find(c => c.key == "frag");
            Assert.AreEqual("grenade", frag.iconName);
            Assert.AreEqual(4f, frag.radius, 0.0001f);
            Assert.AreEqual(1.4f, frag.fuseSeconds, 0.0001f);

            var sticky = result.Consumables.Find(c => c.key == "sticky");
            Assert.AreEqual(180f, sticky.damage, 0.0001f);

            var molotov = result.Consumables.Find(c => c.key == "molotov");
            Assert.AreEqual(5f, molotov.effectSeconds, 0.0001f);

            Assert.AreEqual(2, result.Dances.Count);
            var skill1 = result.Dances.Find(d => d.id == "dance:skill1");
            Assert.AreEqual("Skill_01", skill1.clipName);
        }
    }
}
