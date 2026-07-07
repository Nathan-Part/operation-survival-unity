using System.Collections.Generic;
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public class ItemsImportResult
    {
        public List<AccessoryData> Accessories;
        public List<BoostData> Boosts;
        public List<ConsumableData> Consumables;
        public List<DanceData> Dances;
    }

    public static class ItemsImporter
    {
        public static ItemsImportResult Import(string jsonPath, string accessoriesFolder, string boostsFolder, string consumablesFolder, string dancesFolder, ImportReport report)
        {
            var root = JsonFile.Read<ItemsJsonRoot>(jsonPath);

            var accessories = new List<AccessoryData>();
            accessories.AddRange(root.Accessories.Helmets.Select(e => ImportAccessory(e, AccessoryType.Helmet, accessoriesFolder, report)));
            accessories.AddRange(root.Accessories.Costumes.Select(e => ImportAccessory(e, AccessoryType.Costume, accessoriesFolder, report)));

            var boosts = root.Boosts.Items.Select(e => ImportBoost(e, boostsFolder, report)).ToList();
            var consumables = root.Consumables.Items.Select(e => ImportConsumable(e, consumablesFolder, report)).ToList();
            var dances = root.Dances.Items.Select(e => ImportDance(e, dancesFolder, report)).ToList();

            AssetImportUtil.ReportOrphans<AccessoryData>(accessoriesFolder, accessories.Select(a => a.id), report);
            AssetImportUtil.ReportOrphans<BoostData>(boostsFolder, boosts.Select(b => b.id), report);
            AssetImportUtil.ReportOrphans<ConsumableData>(consumablesFolder, consumables.Select(c => c.key), report);
            AssetImportUtil.ReportOrphans<DanceData>(dancesFolder, dances.Select(d => d.id), report);

            return new ItemsImportResult { Accessories = accessories, Boosts = boosts, Consumables = consumables, Dances = dances };
        }

        static AccessoryData ImportAccessory(AccessoryJsonEntry entry, AccessoryType type, string folder, ImportReport report)
        {
            var accessory = AssetImportUtil.FindOrCreate<AccessoryData>(folder, entry.Id, out var wasCreated);
            var changed = wasCreated;

            Set(ref accessory.id, entry.Id, ref changed);
            Set(ref accessory.displayName, entry.Name, ref changed);
            Set(ref accessory.accessoryType, type, ref changed);
            Set(ref accessory.price, entry.Price, ref changed);
            Set(ref accessory.rarity, EnumParsing.ParseRarity(entry.Rarity), ref changed);
            Set(ref accessory.desc, entry.Desc, ref changed);

            var bonuses = entry.Bonuses.Select(b => new AccessoryBonus { icon = b.Icon, name = b.Name, desc = b.Desc }).ToArray();
            if (!BonusesEqual(accessory.bonuses, bonuses))
            {
                accessory.bonuses = bonuses;
                changed = true;
            }

            Report(report, accessory.id, wasCreated, changed);
            if (changed)
            {
                EditorUtility.SetDirty(accessory);
                AssetDatabase.SaveAssets();
            }
            return accessory;
        }

        static BoostData ImportBoost(BoostJsonEntry entry, string folder, ImportReport report)
        {
            var boost = AssetImportUtil.FindOrCreate<BoostData>(folder, entry.Id, out var wasCreated);
            var changed = wasCreated;

            Set(ref boost.id, entry.Id, ref changed);
            Set(ref boost.slot, EnumParsing.ParseBoostSlot(entry.Slot), ref changed);
            Set(ref boost.displayName, entry.Name, ref changed);
            Set(ref boost.stat, EnumParsing.ParseBoostStat(entry.Stat), ref changed);
            Set(ref boost.bonus, entry.Bonus, ref changed);
            Set(ref boost.price, entry.Price, ref changed);
            Set(ref boost.rarity, EnumParsing.ParseRarity(entry.Rarity), ref changed);

            Report(report, boost.id, wasCreated, changed);
            if (changed)
            {
                EditorUtility.SetDirty(boost);
                AssetDatabase.SaveAssets();
            }
            return boost;
        }

        static ConsumableData ImportConsumable(ConsumableJsonEntry entry, string folder, ImportReport report)
        {
            var consumable = AssetImportUtil.FindOrCreate<ConsumableData>(folder, entry.Key, out var wasCreated);
            var changed = wasCreated;

            Set(ref consumable.key, entry.Key, ref changed);
            Set(ref consumable.displayName, entry.Name, ref changed);
            Set(ref consumable.price, entry.Price, ref changed);
            Set(ref consumable.damage, entry.Damage, ref changed);
            Set(ref consumable.radius, entry.Radius, ref changed);
            Set(ref consumable.fuseSeconds, entry.FuseSeconds, ref changed);
            Set(ref consumable.effectSeconds, entry.EffectSeconds, ref changed);
            Set(ref consumable.throwSpeed, entry.ThrowSpeed, ref changed);
            Set(ref consumable.maxStock, entry.MaxStock, ref changed);
            Set(ref consumable.iconName, entry.Assets.Icon, ref changed);

            Report(report, consumable.key, wasCreated, changed);
            if (changed)
            {
                EditorUtility.SetDirty(consumable);
                AssetDatabase.SaveAssets();
            }
            return consumable;
        }

        static DanceData ImportDance(DanceJsonEntry entry, string folder, ImportReport report)
        {
            var dance = AssetImportUtil.FindOrCreate<DanceData>(folder, entry.Id, out var wasCreated);
            var changed = wasCreated;

            Set(ref dance.id, entry.Id, ref changed);
            Set(ref dance.displayName, entry.Name, ref changed);
            Set(ref dance.clipName, entry.Clip, ref changed);
            Set(ref dance.price, entry.Price, ref changed);
            Set(ref dance.rarity, EnumParsing.ParseRarity(entry.Rarity), ref changed);

            Report(report, dance.id, wasCreated, changed);
            if (changed)
            {
                EditorUtility.SetDirty(dance);
                AssetDatabase.SaveAssets();
            }
            return dance;
        }

        static bool BonusesEqual(AccessoryBonus[] a, AccessoryBonus[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i].icon != b[i].icon || a[i].name != b[i].name || a[i].desc != b[i].desc)
                    return false;
            return true;
        }

        static void Report(ImportReport report, string id, bool wasCreated, bool changed)
        {
            if (wasCreated) report.Created.Add(id);
            else if (changed) report.Updated.Add(id);
            else report.Unchanged.Add(id);
        }

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
