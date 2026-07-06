using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Data.Editor.Import
{
    public static class AssetImportUtil
    {
        public static string AssetPath(string folder, string id) => $"{folder}/{SanitizeFileName(id)}.asset";

        public static string SanitizeFileName(string id) => id.Replace(":", "_").Replace("/", "_");

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parts = folder.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public static T FindById<T>(string folder, string id) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(AssetPath(folder, id));

        public static T FindOrCreate<T>(string folder, string id, out bool wasCreated) where T : ScriptableObject
        {
            EnsureFolder(folder);
            var path = AssetPath(folder, id);
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                wasCreated = false;
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            wasCreated = true;
            return asset;
        }

        public static T FindOrCreateSingleton<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(folder);
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void ReportOrphans<T>(string folder, IEnumerable<string> importedIds, ImportReport report) where T : UnityEngine.Object
        {
            var importedFileNames = new HashSet<string>(importedIds.Select(SanitizeFileName));
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileNameWithoutExtension(path);
                if (!importedFileNames.Contains(fileName))
                    report.Orphaned.Add(path);
            }
        }
    }
}
