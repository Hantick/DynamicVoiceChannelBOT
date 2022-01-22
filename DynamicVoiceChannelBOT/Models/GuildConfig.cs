namespace DynamicVoiceChannelBOT.Models
{
    public class GuildConfig
    {
        public List<EnabledVoiceChannel> EnabledVoiceChannels { get; }

        public GuildConfig()
        {
            EnabledVoiceChannels = new List<EnabledVoiceChannel>();
        }
    }

    public class EnabledVoiceChannel
    {
        public ulong Id { get; set; }
        public string Name { get; set; }

        public EnabledVoiceChannel(ulong id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
