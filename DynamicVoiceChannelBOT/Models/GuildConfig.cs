namespace DynamicVoiceChannelBOT.Models
{
    public class GuildConfig
    {
        public List<ulong> EnabledVoiceChannels { get; }

        public GuildConfig()
        {
            EnabledVoiceChannels = new List<ulong>();
        }
    }
}
