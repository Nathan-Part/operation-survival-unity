using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class AchievementsJsonRoot
    {
        [JsonProperty("achievements")] public List<AchievementJsonEntry> Achievements;
    }

    public class AchievementJsonEntry
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("desc")] public string Desc;
        [JsonProperty("conditionType")] public string ConditionType;
        [JsonProperty("target")] public int Target;
    }
}
