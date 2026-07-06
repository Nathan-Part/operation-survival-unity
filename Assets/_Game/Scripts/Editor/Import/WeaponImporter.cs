using System.Collections.Generic;
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public class WeaponImportResult
    {
        public List<WeaponData> Weapons;
        public UpgradeConfig UpgradeConfig;
    }

    public static class WeaponImporter
    {
        public static WeaponImportResult Import(string jsonPath, string weaponsFolder, string upgradeConfigPath, ImportReport report)
        {
            var root = JsonFile.Read<WeaponsJsonRoot>(jsonPath);
            var weapons = new List<WeaponData>();

            foreach (var entry in root.Weapons)
            {
                var weapon = AssetImportUtil.FindOrCreate<WeaponData>(weaponsFolder, entry.Id, out var wasCreated);
                var changed = wasCreated;

                Set(ref weapon.id, entry.Id, ref changed);
                Set(ref weapon.displayName, entry.Name, ref changed);
                Set(ref weapon.category, EnumParsing.ParseWeaponCategory(entry.Category), ref changed);
                Set(ref weapon.rarity, EnumParsing.ParseRarity(entry.Rarity), ref changed);
                Set(ref weapon.price, entry.Price, ref changed);
                Set(ref weapon.desc, entry.Desc, ref changed);
                Set(ref weapon.semiAuto, entry.SemiAuto, ref changed);
                Set(ref weapon.melee, entry.Melee, ref changed);
                Set(ref weapon.mag, entry.Stats.Mag, ref changed);
                Set(ref weapon.cooldown, entry.Stats.Cooldown, ref changed);
                Set(ref weapon.reload, entry.Stats.Reload, ref changed);
                Set(ref weapon.dmgMult, entry.Stats.DmgMult, ref changed);
                Set(ref weapon.recoil, entry.Stats.Recoil, ref changed);
                Set(ref weapon.range, entry.Stats.Range, ref changed);
                Set(ref weapon.pellets, entry.Stats.Pellets, ref changed);

                if (wasCreated) report.Created.Add(weapon.id);
                else if (changed) report.Updated.Add(weapon.id);
                else report.Unchanged.Add(weapon.id);

                if (changed)
                {
                    EditorUtility.SetDirty(weapon);
                    // Flush immediately: leaving newly-created/changed assets dirty across
                    // the whole loop risks losing in-memory field values to an intervening
                    // asset-database reimport before the trailing SaveAssets() runs.
                    AssetDatabase.SaveAssets();
                }
                weapons.Add(weapon);
            }

            AssetImportUtil.ReportOrphans<WeaponData>(weaponsFolder, weapons.Select(w => w.id), report);

            var upgradeConfig = AssetImportUtil.FindOrCreateSingleton<UpgradeConfig>(upgradeConfigPath);
            upgradeConfig.weaponUpgrades = new[]
            {
                ToSpec("damage", root.Upgrades.Damage),
                ToSpec("fireRate", root.Upgrades.FireRate),
                ToSpec("recoil", root.Upgrades.Recoil),
                ToSpec("mag", root.Upgrades.Mag)
            };
            EditorUtility.SetDirty(upgradeConfig);

            AssetDatabase.SaveAssets();

            return new WeaponImportResult { Weapons = weapons, UpgradeConfig = upgradeConfig };
        }

        static WeaponUpgradeSpec ToSpec(string key, UpgradeSpecJson json) => new()
        {
            statKey = key,
            displayName = json.Name,
            maxLevel = json.Max,
            baseCost = json.BaseCost,
            effectDesc = json.Effect
        };

        static void Set<T>(ref T current, T next, ref bool changed)
        {
            if (!EqualityComparer<T>.Default.Equals(current, next))
            {
                current = next;
                changed = true;
            }
        }
    }
}
