using System.Collections.Generic;
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public class CharacterImportResult
    {
        public List<CharacterData> Characters;
        public MovementConfig MovementConfig;
        public UpgradeConfig UpgradeConfig;
    }

    public static class CharacterImporter
    {
        public static CharacterImportResult Import(string jsonPath, string charactersFolder, string movementConfigPath, string upgradeConfigPath, ImportReport report)
        {
            var root = JsonFile.Read<PlayerJsonRoot>(jsonPath);
            var characters = new List<CharacterData>();

            foreach (var entry in root.Characters)
            {
                var character = AssetImportUtil.FindOrCreate<CharacterData>(charactersFolder, entry.Id, out var wasCreated);
                var changed = wasCreated;

                Set(ref character.id, entry.Id, ref changed);
                Set(ref character.displayName, entry.Name, ref changed);
                Set(ref character.price, entry.Price, ref changed);
                Set(ref character.rarity, EnumParsing.ParseRarity(entry.Rarity), ref changed);
                Set(ref character.desc, entry.Desc, ref changed);
                Set(ref character.baseHealth, entry.Base.Health, ref changed);
                Set(ref character.baseRegen, entry.Base.Regen, ref changed);
                Set(ref character.baseSpeed, entry.Base.Speed, ref changed);

                var skills = entry.Skills.Select(s => new CharacterSkill { icon = s.Icon, name = s.Name, desc = s.Desc }).ToArray();
                if (!SkillsEqual(character.skills, skills))
                {
                    character.skills = skills;
                    changed = true;
                }

                if (wasCreated) report.Created.Add(character.id);
                else if (changed) report.Updated.Add(character.id);
                else report.Unchanged.Add(character.id);

                if (changed)
                {
                    EditorUtility.SetDirty(character);
                    // Flush immediately: leaving newly-created/changed assets dirty across
                    // the whole loop risks losing in-memory field values to an intervening
                    // asset-database reimport before the trailing SaveAssets() runs.
                    AssetDatabase.SaveAssets();
                }
                characters.Add(character);
            }

            AssetImportUtil.ReportOrphans<CharacterData>(charactersFolder, characters.Select(c => c.id), report);

            var movementConfig = AssetImportUtil.FindOrCreateSingleton<MovementConfig>(movementConfigPath);
            movementConfig.walkSpeed = root.Movement.WalkSpeed;
            movementConfig.runSpeed = root.Movement.RunSpeed;
            movementConfig.jumpSpeed = root.Movement.JumpSpeed;
            movementConfig.gravity = root.Movement.Gravity;
            movementConfig.turnLerp = root.Movement.TurnLerp;
            movementConfig.stepUpMax = root.Movement.StepUpMax;
            movementConfig.stepDownSnap = root.Movement.StepDownSnap;
            movementConfig.dash = new DashSettings { distance = root.Movement.Dash.Distance, speed = root.Movement.Dash.Speed, cooldown = root.Movement.Dash.Cooldown };
            movementConfig.stamina = new StaminaSettings { max = root.Movement.Stamina.Max, drainPerSec = root.Movement.Stamina.DrainPerSec, regenPerSec = root.Movement.Stamina.RegenPerSec, minToRun = root.Movement.Stamina.MinToRun };
            movementConfig.fallDamage = new FallDamageSettings { safeHeight = root.Movement.FallDamage.SafeHeight, damagePerMeter = root.Movement.FallDamage.DamagePerMeter, maxDamage = root.Movement.FallDamage.MaxDamage };
            movementConfig.secondWindInvulnSeconds = root.SecondWind.InvulnSeconds;
            movementConfig.secondWindUsesPerRun = root.SecondWind.UsesPerRun;
            EditorUtility.SetDirty(movementConfig);
            AssetDatabase.SaveAssets();

            var upgradeConfig = AssetImportUtil.FindOrCreateSingleton<UpgradeConfig>(upgradeConfigPath);
            upgradeConfig.playerUpgrades = new[]
            {
                ToSpec("health", root.PlayerUpgrades.Health),
                ToSpec("regen", root.PlayerUpgrades.Regen),
                ToSpec("speed", root.PlayerUpgrades.Speed)
            };
            EditorUtility.SetDirty(upgradeConfig);
            AssetDatabase.SaveAssets();

            return new CharacterImportResult { Characters = characters, MovementConfig = movementConfig, UpgradeConfig = upgradeConfig };
        }

        static PlayerUpgradeSpec ToSpec(string key, PlayerUpgradeSpecJson json) => new()
        {
            statKey = key,
            displayName = json.Name,
            maxLevel = json.Max,
            baseCost = json.BaseCost,
            perLevel = json.PerLevel
        };

        static bool SkillsEqual(CharacterSkill[] a, CharacterSkill[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i].icon != b[i].icon || a[i].name != b[i].name || a[i].desc != b[i].desc)
                    return false;
            return true;
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
