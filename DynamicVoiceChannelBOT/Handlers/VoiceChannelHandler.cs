using Discord;
using Discord.WebSocket;
using DynamicVoiceChannelBOT.Services;
using Serilog;

namespace DynamicVoiceChannelBOT.Handlers
{
    public class VoiceChannelHandler
    {
        private readonly GuildConfigService _guildConfigService;

        public VoiceChannelHandler(GuildConfigService guildConfigService)
        {
            _guildConfigService = guildConfigService;
        }

        public async Task HandleVoiceStateUpdated(SocketUser user, SocketVoiceState beforeVoiceState, SocketVoiceState afterVoiceState)
        {
            var guild = beforeVoiceState.VoiceChannel?.Guild ?? afterVoiceState.VoiceChannel?.Guild;
            if (guild == null)
            {
                Log.Warning("Unable to define a guild from voice channels states.");
                return;
            }

            var enabledChannels = _guildConfigService.GetGuildConfig(guild.Id).EnabledVoiceChannels;
            if (enabledChannels.Count == 0) return;

            // User joined the channel
            if (afterVoiceState.VoiceChannel != null)
            {
                if (afterVoiceState.VoiceChannel.UserLimit == null) return;

                var possibleVCToCopy = afterVoiceState.VoiceChannel;
                if (enabledChannels.Any(c => c.Id == possibleVCToCopy.Id || c.Name == possibleVCToCopy.Name.Replace("💨", ""))
                    && IsVoiceChannelFull(possibleVCToCopy)
                    && AreCopiedChannelsFull(guild.VoiceChannels, possibleVCToCopy.Name))
                {
                    await CloneChannel(possibleVCToCopy);
                }
            }
            // User leaved the channel
            if (beforeVoiceState.VoiceChannel != null)
            {
                if (beforeVoiceState.VoiceChannel.UserLimit == null) return;

                if (afterVoiceState.VoiceChannel != null && IsVoiceChannelFull(afterVoiceState.VoiceChannel))
                    return;
                var possibleChannelToDelete = beforeVoiceState.VoiceChannel;
                if (possibleChannelToDelete.Name.EndsWith("💨")
                    && IsVoiceChannelEmpty(possibleChannelToDelete))
                {
                    await possibleChannelToDelete.DeleteAsync();
                }
                else if (!possibleChannelToDelete.Name.EndsWith("💨")
                    && enabledChannels.Any(ch => ch.Id == possibleChannelToDelete.Id)
                    && IsVoiceChannelEmpty(possibleChannelToDelete))
                {
                    var duplicatedChannel = guild.VoiceChannels.FirstOrDefault(ch => ch.Name == possibleChannelToDelete.Name + "💨");
                    if (duplicatedChannel != null)
                        await duplicatedChannel.DeleteAsync();
                }
            }
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        private async Task CloneChannel(SocketVoiceChannel possibleVCToCopy)
        {
            await possibleVCToCopy.Guild.CreateVoiceChannelAsync(
                possibleVCToCopy.Name.EndsWith("💨") ? possibleVCToCopy.Name : possibleVCToCopy.Name + "💨",
                new Action<VoiceChannelProperties>((newVoiceChannel) =>
                {
                    newVoiceChannel.Bitrate = possibleVCToCopy.Bitrate;
                    newVoiceChannel.CategoryId = possibleVCToCopy.CategoryId;
                    newVoiceChannel.Position = possibleVCToCopy.Position;
                    newVoiceChannel.UserLimit = possibleVCToCopy.UserLimit;
                }));
        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        private async Task DeleteDuplicatedChannelsAsync(SocketGuild guild)
        {
            var duplicateChs = guild.VoiceChannels.Where(x => x.Name.Contains("💨")).GroupBy(x => x.Name)
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

        private bool IsVoiceChannelFull(SocketVoiceChannel ch) => ch.UserLimit == ch.Users.Count;
        private bool IsVoiceChannelEmpty(SocketVoiceChannel ch) => ch.Users == null || ch.Users.Count == 0;
        private bool AreCopiedChannelsFull(IReadOnlyCollection<SocketVoiceChannel> voiceChannels, string channelName)
            => voiceChannels.All(c => c.Name.Contains(channelName.Replace("💨", "")) && IsVoiceChannelFull(c));
    }
}
