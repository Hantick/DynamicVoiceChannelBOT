using Newtonsoft.Json;

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
            if (!File.Exists($"{key}.json") && !File.Exists(key))
                return default;
            string json;
            if(key.Contains(".json"))
                json = File.ReadAllText(key);
            else
                json = File.ReadAllText($"{key}.json").TrimEnd();
            var obj = JsonConvert.DeserializeObject<T>(json);
            return obj;
        }

        public void StoreObject(object obj, string key)
        {
            var file = $"{key}.json";
            Directory.CreateDirectory(Path.GetDirectoryName(file));
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
