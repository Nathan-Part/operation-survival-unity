using System.IO;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import
{
    public static class JsonFile
    {
        public static T Read<T>(string path) => JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
    }
}
