using System.Collections.Generic;
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public class PowerUpImportResult
    {
        public List<PowerUpData> PowerUps;
        public PowerUpConfig Config;
    }

    public static class PowerUpImporter
    {
        public static PowerUpImportResult Import(string jsonPath, string powerUpsFolder, string configPath, ImportReport report)
        {
            var root = JsonFile.Read<PowerUpsJsonRoot>(jsonPath);
            var powerUps = new List<PowerUpData>();

            foreach (var kvp in root.PowerUps)
            {
                var id = kvp.Key;
                var entry = kvp.Value;
                var powerUp = AssetImportUtil.FindOrCreate<PowerUpData>(powerUpsFolder, id, out var wasCreated);
                var changed = wasCreated;

                Set(ref powerUp.id, id, ref changed);
                Set(ref powerUp.displayName, entry.Name, ref changed);
                Set(ref powerUp.colorHex, entry.Color, ref changed);
                Set(ref powerUp.duration, entry.Duration ?? 0f, ref changed);
                Set(ref powerUp.isInstant, entry.Instant ?? false, ref changed);
                Set(ref powerUp.effectDesc, entry.Effect, ref changed);
                Set(ref powerUp.pvpPolicy, EnumParsing.ParsePvpPolicy(entry.Pvp), ref changed);

                if (wasCreated) report.Created.Add(powerUp.id);
                else if (changed) report.Updated.Add(powerUp.id);
                else report.Unchanged.Add(powerUp.id);

                if (changed)
                {
                    EditorUtility.SetDirty(powerUp);
                    // Flush immediately: leaving newly-created/changed assets dirty across
                    // the whole loop risks losing in-memory field values to an intervening
                    // asset-database reimport before the trailing SaveAssets() runs.
                    AssetDatabase.SaveAssets();
                }
                powerUps.Add(powerUp);
            }

            AssetImportUtil.ReportOrphans<PowerUpData>(powerUpsFolder, powerUps.Select(p => p.id), report);

            var config = AssetImportUtil.FindOrCreateSingleton<PowerUpConfig>(configPath);
            config.dropChance = root.DropChance;
            config.dropChanceLootMaster = root.DropChanceLootMaster;
            config.pickupRadius = root.PickupRadius;
            config.defaultDuration = root.DefaultDuration;
            config.instantHealAmount = root.InstantConstants.InstantHealAmount;
            config.novaRadius = root.InstantConstants.NovaRadius;
            config.novaDamage = root.InstantConstants.NovaDamage;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            return new PowerUpImportResult { PowerUps = powerUps, Config = config };
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
