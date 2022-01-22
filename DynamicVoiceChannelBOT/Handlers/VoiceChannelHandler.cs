using Discord;
using Discord.WebSocket;
using DynamicVoiceChannelBOT.Services;
using Serilog;

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

        internal async Task HandleVoiceStateUpdated(SocketUser user, SocketVoiceState beforeVoiceState, SocketVoiceState afterVoiceState)
        {
            var guildId = beforeVoiceState.VoiceChannel?.Guild.Id ?? afterVoiceState.VoiceChannel?.Guild.Id;
            if (!guildId.HasValue)
            {
                Log.Warning("Unable to define a guild from voice channels states.");
                return;
            }

            var enabledChannels = _guildConfigService.GetGuildConfig(guildId.Value).EnabledVoiceChannels;
            if (!enabledChannels.Any()) return;

            // User joined the channel
            if (afterVoiceState.VoiceChannel != null)
            {
                var possibleVoiceChannelToCopy = afterVoiceState.VoiceChannel;
                if (possibleVoiceChannelToCopy.UserLimit != null
                    && enabledChannels.Contains(possibleVoiceChannelToCopy.Id))
                {
                    await CloneChannelIfNeeded(afterVoiceState.VoiceChannel);
                }
            }
            // User leaved the channel
            if (beforeVoiceState.VoiceChannel != null)
            {
                var possibleChannelToDelete = beforeVoiceState.VoiceChannel;
                if (possibleChannelToDelete.Name.EndsWith("💨")
                    && possibleChannelToDelete.UserLimit != null
                    && (possibleChannelToDelete.Users == null
                    || possibleChannelToDelete.Users.Count == 0))
                {
                    await possibleChannelToDelete.DeleteAsync();
                    //await DeleteDuplicatedChannelsAsync(possibleChannelToDelete.Guild);
                }
            }
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        private async Task CloneChannelIfNeeded(SocketVoiceChannel possibleVoiceChannelToCopy)
        {
            if (possibleVoiceChannelToCopy.UserLimit < possibleVoiceChannelToCopy?.Users?.Count
                && possibleVoiceChannelToCopy.Guild.VoiceChannels
                .Any(c => c.Name == possibleVoiceChannelToCopy?.Name + "💨" && c.Users?.Count < c.UserLimit))
                return;

            await possibleVoiceChannelToCopy.Guild.CreateVoiceChannelAsync(possibleVoiceChannelToCopy.Name + "💨",
                new Action<VoiceChannelProperties>((newVoiceChannel) =>
                {
                    newVoiceChannel.Bitrate = possibleVoiceChannelToCopy.Bitrate;
                    newVoiceChannel.CategoryId = possibleVoiceChannelToCopy.CategoryId;
                    newVoiceChannel.Name = possibleVoiceChannelToCopy.Name + "💨";
                    newVoiceChannel.Position = possibleVoiceChannelToCopy.Position;
                    newVoiceChannel.UserLimit = possibleVoiceChannelToCopy.UserLimit;
                }));
        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        private async Task CheckAndDeleteDuplicatedChannels(SocketGuild guild)
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
