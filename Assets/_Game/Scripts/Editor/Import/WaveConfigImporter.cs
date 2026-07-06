using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public static class WaveConfigImporter
    {
        public static WaveConfig Import(string jsonPath, string configPath, ImportReport report)
        {
            var root = JsonFile.Read<WavesJsonRoot>(jsonPath);
            var config = AssetImportUtil.FindOrCreateSingleton<WaveConfig>(configPath);

            config.maxEnemiesPerWave = root.Constants.MaxEnemiesPerWave;
            config.intermissionSeconds = root.Constants.IntermissionSeconds;
            config.bossEvery = root.Constants.BossEvery;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            report.Updated.Add("WaveConfig");

            return config;
        }
    }
}
