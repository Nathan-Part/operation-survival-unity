using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class ContractsJsonRoot
    {
        [JsonProperty("contracts")] public List<ContractJsonEntry> Contracts;
    }

    public class ContractJsonEntry
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("desc")] public string Desc;
        [JsonProperty("type")] public string Type;
        [JsonProperty("target")] public int Target;
        [JsonProperty("reward")] public int Reward;
    }
}
