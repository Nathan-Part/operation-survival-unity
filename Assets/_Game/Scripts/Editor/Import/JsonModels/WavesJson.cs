using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class WavesJsonRoot
    {
        [JsonProperty("constants")] public WaveConstantsJson Constants;
    }

    public class WaveConstantsJson
    {
        [JsonProperty("maxEnemiesPerWave")] public int MaxEnemiesPerWave;
        [JsonProperty("intermissionSeconds")] public float IntermissionSeconds;
        [JsonProperty("bossEvery")] public int BossEvery;
    }
}
