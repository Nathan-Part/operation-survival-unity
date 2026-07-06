using System.Collections.Generic;
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public class EnemyImportResult
    {
        public List<EnemyData> Enemies;
        public EnemyCombatConfig CombatConfig;
    }

    public static class EnemyImporter
    {
        public static EnemyImportResult Import(string jsonPath, string enemiesFolder, string weaponsFolder, string combatConfigPath, ImportReport report)
        {
            var root = JsonFile.Read<EnemiesJsonRoot>(jsonPath);
            var enemies = new List<EnemyData>();

            foreach (var kvp in root.Types)
            {
                var id = kvp.Key;
                var entry = kvp.Value;
                var enemy = AssetImportUtil.FindOrCreate<EnemyData>(enemiesFolder, id, out var wasCreated);
                var changed = wasCreated;

                Set(ref enemy.id, id, ref changed);
                Set(ref enemy.displayName, entry.Name, ref changed);

                var hpMult = entry.HpMult ?? WarnDefault(report, id, nameof(entry.HpMult), 1f);
                Set(ref enemy.hpMult, hpMult, ref changed);

                var speedMult = entry.SpeedMult ?? WarnDefault(report, id, nameof(entry.SpeedMult), 1f);
                Set(ref enemy.speedMult, speedMult, ref changed);

                var scale = entry.Scale ?? WarnDefault(report, id, nameof(entry.Scale), 1f);
                Set(ref enemy.scale, scale, ref changed);

                Set(ref enemy.melee, entry.Melee, ref changed);

                var damageMult = entry.DamageMult ?? WarnDefault(report, id, nameof(entry.DamageMult), 1f);
                Set(ref enemy.damageMult, damageMult, ref changed);

                var burst = entry.Burst ?? (int)WarnDefault(report, id, nameof(entry.Burst), 1f);
                Set(ref enemy.burst, burst, ref changed);

                var weapon = entry.Weapon != null ? AssetImportUtil.FindById<WeaponData>(weaponsFolder, entry.Weapon) : null;
                Set(ref enemy.weapon, weapon, ref changed);

                Set(ref enemy.radarColor, entry.RadarColor, ref changed);
                Set(ref enemy.glow, entry.Glow, ref changed);
                Set(ref enemy.armor, EnumParsing.ParseArmor(entry.Armor), ref changed);
                Set(ref enemy.opacity, entry.Opacity ?? 1f, ref changed);
                Set(ref enemy.hasShootOverride, entry.ShootCd.HasValue || entry.ShootRange.HasValue, ref changed);
                Set(ref enemy.shootCdOverride, entry.ShootCd ?? 0f, ref changed);
                Set(ref enemy.shootRangeOverride, entry.ShootRange ?? 0f, ref changed);
                Set(ref enemy.behavior, entry.Behavior, ref changed);

                if (wasCreated) report.Created.Add(enemy.id);
                else if (changed) report.Updated.Add(enemy.id);
                else report.Unchanged.Add(enemy.id);

                if (changed)
                {
                    EditorUtility.SetDirty(enemy);
                    // Flush immediately: leaving newly-created/changed assets dirty across
                    // the whole loop risks losing in-memory field values to an intervening
                    // asset-database reimport before the trailing SaveAssets() runs.
                    AssetDatabase.SaveAssets();
                }
                enemies.Add(enemy);
            }

            AssetImportUtil.ReportOrphans<EnemyData>(enemiesFolder, enemies.Select(e => e.id), report);

            var combatConfig = AssetImportUtil.FindOrCreateSingleton<EnemyCombatConfig>(combatConfigPath);
            combatConfig.baseStats = new EnemyBaseStats
            {
                hp = root.BaseStats.Hp, walkSpeed = root.BaseStats.WalkSpeed, runSpeed = root.BaseStats.RunSpeed,
                radius = root.BaseStats.Radius, turnSpeed = root.BaseStats.TurnSpeed, shootRange = root.BaseStats.ShootRange,
                fireCooldown = root.BaseStats.FireCooldown, accuracy = root.BaseStats.Accuracy, damage = root.BaseStats.Damage,
                burstGap = root.BaseStats.BurstGap, detectRadius = root.BaseStats.DetectRadius, loseRadius = root.BaseStats.LoseRadius,
                loseTime = root.BaseStats.LoseTime, meleeRange = root.BaseStats.MeleeRange, meleeDamage = root.BaseStats.MeleeDamage,
                meleeCooldown = root.BaseStats.MeleeCooldown
            };
            combatConfig.hitZones = new HitZoneSettings
            {
                headAboveY = root.HitZones.Head.AboveY ?? 0f,
                headDamage = root.HitZones.Head.Damage,
                torsoDamage = root.HitZones.Torso.Damage,
                legsBelowY = root.HitZones.Legs.BelowY ?? 0f,
                legsDamage = root.HitZones.Legs.Damage
            };
            combatConfig.vipMoneyMult = root.Vip.MoneyMult;
            combatConfig.vipMinWave = root.Vip.MinWave;
            combatConfig.vipExcludedTypes = root.Vip.Excluded.ToArray();
            combatConfig.bossKillBonus = root.SpecialConstants.BossKillBonus;
            EditorUtility.SetDirty(combatConfig);

            AssetDatabase.SaveAssets();

            return new EnemyImportResult { Enemies = enemies, CombatConfig = combatConfig };
        }

        static float WarnDefault(ImportReport report, string id, string field, float defaultValue)
        {
            report.Warnings.Add($"{id}: missing {field}, defaulted to {defaultValue}");
            return defaultValue;
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
