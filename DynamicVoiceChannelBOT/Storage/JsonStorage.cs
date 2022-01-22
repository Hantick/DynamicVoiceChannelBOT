using Newtonsoft.Json;
using Serilog;

namespace DynamicVoiceChannelBOT.Storage
{
    public class JsonStorage : IDataStorage
    {

        readonly char[] illegalChars = { '\\', '"', '?', ':', '*', '|', '<', '>' };
        public void DeleteObject(string key)
        {
            File.Delete($"{key}.json");    
        }

        public bool Exists(string filePath)
        {
            return File.Exists(filePath.Replace("\"", "").Trim(illegalChars) + ".json");
        }

        public T RestoreObject<T>(string key)
        {
#pragma warning disable CS8603 // Possible null reference return.
            if (!File.Exists($"{key}.json") && !File.Exists(key))
                return default;
            string json;
            if(key.Contains(".json"))
                json = File.ReadAllText(key);
            else
                json = File.ReadAllText($"{key}.json").TrimEnd();
            var obj = JsonConvert.DeserializeObject<T>(json);
            return obj;
#pragma warning restore CS8603 // Possible null reference return.
        }

        public void StoreObject(object obj, string key)
        {
            var file = $"{key}.json";
            var path = Path.GetDirectoryName(file);

#pragma warning disable CS8604 // Possible null reference argument.
            Directory.CreateDirectory(path);
#pragma warning restore CS8604 // Possible null reference argument.
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(file, json);
        }

        public void UpdateObject(object obj, string oldPath, string newPath)
        {
            newPath = newPath.Replace("\"", "").Trim(illegalChars);
            if (Exists($"{oldPath}.json"))
                File.Move($"{oldPath}.json", ($"{newPath}.json"));
        }
    }
}
