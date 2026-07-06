using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Data.Save
{
    /// Chargement/écriture du save (parité web loadSave/persist) : merge sur les
    /// défauts (un champ absent garde sa valeur par défaut), garanties de
    /// migration (knife possédé, medkit présent), JSON corrompu → save neuve.
    public static class SaveMigration
    {
        public static SaveData FromJson(string json)
        {
            var save = new SaveData(); // porte tous les défauts web
            if (string.IsNullOrWhiteSpace(json)) return save;
            try
            {
                var parsed = JToken.Parse(json);
                if (parsed.Type != JTokenType.Object) return new SaveData();
                // merge champ à champ : les clés présentes écrasent les défauts,
                // les collections fournies REMPLACENT (comme Object.assign du web)
                JsonConvert.PopulateObject(json, save, new JsonSerializerSettings
                {
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    Error = (_, args) => args.ErrorContext.Handled = true // champ illisible → défaut conservé
                });
                if (save.settings == null) save.settings = new SaveSettings();
                if (save.lifetime == null) save.lifetime = new SaveLifetime();
                if (save.ownedWeapons == null) save.ownedWeapons = new SaveData().ownedWeapons;
                if (save.consumables == null) save.consumables = new SaveData().consumables;
                if (!save.ownedWeapons.Contains("knife")) save.ownedWeapons.Add("knife");       // web #50
                if (!save.consumables.ContainsKey("medkit")) save.consumables["medkit"] = 3;    // web #178
                return save;
            }
            catch (JsonException)
            {
                return new SaveData();
            }
        }

        public static string ToJson(SaveData save) => JsonConvert.SerializeObject(save);
    }
}
