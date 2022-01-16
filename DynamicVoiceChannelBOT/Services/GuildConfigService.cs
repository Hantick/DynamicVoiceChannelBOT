using DynamicVoiceChannelBOT.Models;
using DynamicVoiceChannelBOT.Storage;

namespace DynamicVoiceChannelBOT.Services
{
    public class GuildConfigService
    {
        private readonly IDataStorage _storage;
        private const string PATH = "GuildConfigs";
        public GuildConfigService(IDataStorage storage)
        {
            _storage = storage;
        }

        public GuildConfig GetGuildConfig(ulong guildId) => _storage.RestoreObject<GuildConfig>($"{PATH}/{guildId}");

        public void CreateGuildConfig(ulong guildId) => _storage.StoreObject(new GuildConfig(), $"{PATH}/{guildId}");

        public void SaveGuildConfig(GuildConfig config, ulong guildId) => _storage.StoreObject(config, $"{PATH}/{guildId}");
    }
}
