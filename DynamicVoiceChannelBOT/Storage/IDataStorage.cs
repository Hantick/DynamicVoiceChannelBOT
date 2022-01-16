namespace DynamicVoiceChannelBOT.Storage
{
    public interface IDataStorage
    {
        void StoreObject(object obj, string key);
        T RestoreObject<T>(string key);
        void DeleteObject(string key);
        void UpdateObject(object obj, string oldPath, string newPath);
        bool Exists(string filePath);
    }
}
