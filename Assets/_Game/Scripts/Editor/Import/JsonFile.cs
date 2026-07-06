using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Game.Data.Editor.Import
{
    public static class JsonFile
    {
        public static T Read<T>(string path)
        {
            var token = JToken.Parse(File.ReadAllText(path));
            StripDocKeys(token);
            return token.ToObject<T>();
        }

        static void StripDocKeys(JToken token)
        {
            if (token is JObject obj)
            {
                obj.Remove("_doc");
                foreach (var property in obj.Properties().ToList())
                    StripDocKeys(property.Value);
            }
            else if (token is JArray array)
            {
                foreach (var item in array)
                    StripDocKeys(item);
            }
        }
    }
}
