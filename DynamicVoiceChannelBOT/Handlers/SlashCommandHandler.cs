using Discord;
using Discord.WebSocket;
using DynamicVoiceChannelBOT.Models;
using DynamicVoiceChannelBOT.Services;
using DynamicVoiceChannelBOT.Storage;
using Serilog;

namespace DynamicVoiceChannelBOT.Handlers
{
    public class SlashCommandHandler
    {
        //Commands
        public const string COMMAND_CHANNELS = "channels";
        public const string COMMAND_ADD_CHANNEL = "add-channel";
        public const string COMMAND_SYNC_CHANNELS = "channels-sync";

        private readonly GuildConfigService _guildConfigService;

        public SlashCommandHandler(GuildConfigService guildConfigService)
        {
            _guildConfigService = guildConfigService;
        }

        public Task OnSlashCommand(SocketSlashCommand command)
        {
            var guild = (command.Channel as SocketGuildChannel)?.Guild;
            if (guild == null)
            {
                Log.Warning("No guild retrieved from executing channels slash command.");
                return Task.CompletedTask;
            }

            var guildUser = (command.User as SocketGuildUser);
            if (guildUser == null) return Task.CompletedTask;
            if (!guildUser.GuildPermissions.ManageChannels)
            {
                command.RespondAsync($"You have no access to use this commands. Manage Channels permission needed.");
            }

            switch (command.CommandName)
            {
                case COMMAND_CHANNELS:
                    HandleChannels(command, guild.Id);
                    break;
                case COMMAND_ADD_CHANNEL:
                    HandleAddChannels(command, guild.Id);
                    break;
                case COMMAND_SYNC_CHANNELS:
                    HandleSyncChannels(command, guild);
                    break;
            }
            return Task.CompletedTask;
        }

        private void HandleSyncChannels(SocketSlashCommand command, SocketGuild guild)
        {
            var config = _guildConfigService.GetGuildConfig(guild.Id);

            var channelsToRemoveFromConfig = new HashSet<ulong>();
            int syncedChannels = 0;
            foreach (var configChannel in config.EnabledVoiceChannels)
            {
                var guildChannel = guild.VoiceChannels.FirstOrDefault(ch => ch.Id == configChannel.Id);
                if (guildChannel == null)
                {
                    channelsToRemoveFromConfig.Add(configChannel.Id);
                    syncedChannels++;
                    continue;
                }

                if (guildChannel.Name == configChannel.Name) continue;

                configChannel.Name = guildChannel.Name;
                syncedChannels++;
            }
            config.EnabledVoiceChannels.RemoveAll(x => channelsToRemoveFromConfig.Contains(x.Id));

            _guildConfigService.SaveGuildConfig(config, guild.Id);
            Log.Information($"Synced {syncedChannels} voice channels for Guild {guild.Id}.");
            command.RespondAsync($"Successfully synced {syncedChannels} voice channels.");
        }

        private void HandleAddChannels(SocketSlashCommand command, ulong guildId)
        {
            var channelType = command.Data?.Options
                .FirstOrDefault(c => c.Type == ApplicationCommandOptionType.Channel);
            if (channelType == null) return;

            var channel = (SocketGuildChannel)channelType.Value;

            var config = _guildConfigService.GetGuildConfig(guildId);
            if (channel.GetChannelType() == ChannelType.Text)
            {
                command.RespondAsync("You can't add text channels");
                return;
            }
            if (config.EnabledVoiceChannels.Any(c => c.Id == channel.Id))
            {
                command.RespondAsync("This voice channel is already added.");
                return;
            }
            if (((SocketVoiceChannel)channel).UserLimit == null)
            {
                command.RespondAsync("You can add only voice channels with user limit.");
                return;
            }

            config.EnabledVoiceChannels.Add(new EnabledVoiceChannel(channel.Id, channel.Name));
            _guildConfigService.SaveGuildConfig(config, guildId);
            command.RespondAsync($"Voice channel {MentionUtils.MentionChannel(channel.Id)} added successfully.");
        }

        private void HandleChannels(SocketSlashCommand command, ulong guildId)
        {
            var config = _guildConfigService.GetGuildConfig(guildId);

            if (config == null) throw new NullReferenceException($"No config file found for Guild {guildId}");
            if (config.EnabledVoiceChannels == null || config.EnabledVoiceChannels.Count == 0)
                command.RespondAsync("There are no channels set yet.");

#pragma warning disable CS8604 // Possible null reference argument.
            command.RespondAsync(String.Join("\n", config.EnabledVoiceChannels.Select(v => MentionUtils.MentionChannel(v.Id))));
#pragma warning restore CS8604 // Possible null reference argument.
        }
    }
}
