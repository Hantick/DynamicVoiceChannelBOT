using Discord;
using Discord.WebSocket;
using DynamicVoiceChannelBOT.Services;

namespace DynamicVoiceChannelBOT.Handlers
{
    public class VoiceChannelHandler
    {
        private readonly GuildConfigService _guildConfigService;
        //private readonly DiscordSocketClient _client;
        public VoiceChannelHandler(GuildConfigService guildConfigService)
        {
            _guildConfigService = guildConfigService;
        }

        internal async Task HandleVoiceStateUpdated(SocketUser user, SocketVoiceState voiceState, SocketVoiceState afterVoiceState)
        {
            var voiceChannel = voiceState.VoiceChannel ?? afterVoiceState.VoiceChannel; //Get first voiceState then afterVoice state
            var guildId = voiceChannel.Guild.Id;
            var enabledChannels = _guildConfigService.GetGuildConfig(guildId).EnabledVoiceChannels;
            if (!enabledChannels.Contains(voiceChannel.Id)) return;

            if (voiceChannel == null) return;

            //if (voiceChannel.Id == 802905505365491743 && voiceChannel.Users.Count > 0)
            //{
            //    await voiceChannel.ConnectAsync(false, true);
            //}
            //else if(voiceChannel.Id == 802905505365491743 && voiceChannel.Users.Count == 0)
            //{
            //    await voiceChannel.DisconnectAsync();
            //}

            await DeleteDuplicatedChannelsAsync(voiceChannel.Guild);

            if (voiceChannel.UserLimit < voiceChannel?.Users?.Count
                && voiceChannel.Guild.VoiceChannels.Any(c => c.Name == voiceChannel?.Name && c.Users?.Count < c.UserLimit))
                return;

            await voiceChannel.Guild.CreateVoiceChannelAsync(voiceChannel.Name, new Action<VoiceChannelProperties>((VoiceChannelProperties) =>
            {
                VoiceChannelProperties.Bitrate = voiceChannel.Bitrate;
                VoiceChannelProperties.CategoryId = voiceChannel.CategoryId;
                VoiceChannelProperties.Name = voiceChannel.Name + "💨";
                VoiceChannelProperties.Position = voiceChannel.Position;
                VoiceChannelProperties.UserLimit = voiceChannel.UserLimit;
            }));

        }

        private async Task DeleteDuplicatedChannelsAsync(SocketGuild guild)
        {
            var duplicateChs = guild.VoiceChannels.GroupBy(x => x.Name)
               .Where(x => x.Count() > 1)
               .Select(x => new { Name = x.Key, chs = x.ToList() }).ToList();

            if (duplicateChs.Count == 0) return;

            foreach (var dupCh in duplicateChs)
            {
                var emptyDupChs = dupCh.chs.Where(c => c.Users.Count == 0).ToList();
                if (emptyDupChs.Count == 0 || emptyDupChs.Count == 1)
                {
                    continue;
                }
                else if (emptyDupChs.Count > 1)
                {
                    emptyDupChs.RemoveAt(0);
                    foreach (var ch in emptyDupChs)
                    {
                        if (ch.Users.Count > 0) continue;
                        await ch.DeleteAsync();
                    }
                }
            }

        }
    }
}
