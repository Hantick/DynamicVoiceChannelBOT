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

        private readonly GuildConfigService _guildConfigService;

        public SlashCommandHandler(GuildConfigService guildConfigService)
        {
            _guildConfigService = guildConfigService;
        }

        public Task OnSlashCommand(SocketSlashCommand command)
        {
            switch (command.CommandName)
            {
                case COMMAND_CHANNELS:
                    HandleChannels(command);
                    break;
                case COMMAND_ADD_CHANNEL:
                    HandleAddChannels(command);
                    break;
            }
            return Task.CompletedTask;
        }

        private void HandleAddChannels(SocketSlashCommand command)
        {
            var channelType = command.Data?.Options
                .FirstOrDefault(c => c.Type == ApplicationCommandOptionType.Channel);
            if (channelType == null) return;

            var channel = (SocketGuildChannel)channelType.Value;
            var guildId = channel.Guild.Id;
            var config = _guildConfigService.GetGuildConfig(guildId);
            if (channel.GetChannelType() == ChannelType.Text)
            {
                command.RespondAsync("You can't add text channels");
                return;
            }
            if (config.EnabledVoiceChannels.Contains(channel.Id))
            {
                command.RespondAsync("This voice channel is already added.");
                return;
            }
            if (((SocketVoiceChannel)channel).UserLimit == null)
            {
                command.RespondAsync("You can add only voice channels with user limit.");
                return;
            }

            config.EnabledVoiceChannels.Add(channel.Id);
            _guildConfigService.SaveGuildConfig(config, guildId);
            command.RespondAsync($"Voice channel {MentionUtils.MentionChannel(channel.Id)} added successfully.");
        }

        private void HandleChannels(SocketSlashCommand command)
        {
            var guildId = (command.Channel as SocketGuildChannel)?.Guild?.Id;
            if (guildId == null)
            {
                Log.Warning("No guild retrieved from executing channels slash command.");
                return;
            }
            var config = _guildConfigService.GetGuildConfig(guildId.Value);

            if (config == null) throw new NullReferenceException($"No config file found for Guild {guildId.Value}");
            if (config.EnabledVoiceChannels == null || config.EnabledVoiceChannels.Count == 0)
                command.RespondAsync("There are no channels set yet.");

#pragma warning disable CS8604 // Possible null reference argument.
            command.RespondAsync(String.Join("\n", config.EnabledVoiceChannels.Select(v => MentionUtils.MentionChannel(v))));
#pragma warning restore CS8604 // Possible null reference argument.
        }
    }
}
