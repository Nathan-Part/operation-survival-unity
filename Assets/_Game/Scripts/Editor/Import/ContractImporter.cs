using System.Collections.Generic;
using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public static class ContractImporter
    {
        public static List<ContractData> Import(string jsonPath, string folder, ImportReport report)
        {
            var root = JsonFile.Read<ContractsJsonRoot>(jsonPath);

            var contracts = root.Contracts.Select(e => ImportOne(e, folder, report)).ToList();

            AssetImportUtil.ReportOrphans<ContractData>(folder, contracts.Select(c => c.id), report);

            return contracts;
        }

        static ContractData ImportOne(ContractJsonEntry entry, string folder, ImportReport report)
        {
            var contract = AssetImportUtil.FindOrCreate<ContractData>(folder, entry.Id, out var wasCreated);
            var changed = wasCreated;

            Set(ref contract.id, entry.Id, ref changed);
            Set(ref contract.desc, entry.Desc, ref changed);
            Set(ref contract.type, entry.Type, ref changed);
            Set(ref contract.target, entry.Target, ref changed);
            Set(ref contract.reward, entry.Reward, ref changed);

            Report(report, contract.id, wasCreated, changed);
            if (changed)
            {
                EditorUtility.SetDirty(contract);
                AssetDatabase.SaveAssets();
            }
            return contract;
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
