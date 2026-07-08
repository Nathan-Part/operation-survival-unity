using System.Collections.Generic;
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public static class AchievementImporter
    {
        public static List<AchievementData> Import(string jsonPath, string folder, ImportReport report)
        {
            var root = JsonFile.Read<AchievementsJsonRoot>(jsonPath);

            var achievements = root.Achievements.Select(e => ImportOne(e, folder, report)).ToList();

            AssetImportUtil.ReportOrphans<AchievementData>(folder, achievements.Select(a => a.id), report);

            return achievements;
        }

        static AchievementData ImportOne(AchievementJsonEntry entry, string folder, ImportReport report)
        {
            var achievement = AssetImportUtil.FindOrCreate<AchievementData>(folder, entry.Id, out var wasCreated);
            var changed = wasCreated;

            Set(ref achievement.id, entry.Id, ref changed);
            Set(ref achievement.displayName, entry.Name, ref changed);
            Set(ref achievement.desc, entry.Desc, ref changed);
            Set(ref achievement.conditionType, entry.ConditionType, ref changed);
            Set(ref achievement.target, entry.Target, ref changed);

            Report(report, achievement.id, wasCreated, changed);
            if (changed)
            {
                EditorUtility.SetDirty(achievement);
                AssetDatabase.SaveAssets();
            }
            return achievement;
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
