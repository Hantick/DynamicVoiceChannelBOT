using Discord;
using Discord.Net;
using Discord.WebSocket;
using DynamicVoiceChannelBOT;
using DynamicVoiceChannelBOT.Handlers;
using DynamicVoiceChannelBOT.Models;
using DynamicVoiceChannelBOT.Services;
using DynamicVoiceChannelBOT.Storage;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DynamicVoiceChannelBOT
{
    public class DiscordBot
    {
        public bool IsRunning { get; private set; }

        private readonly IConfiguration _configuration;
        private readonly GuildConfigService _guildConfigService;
        private CancellationTokenSource _source { get; set; }
        private readonly DiscordSocketClient _client;

        //Handlers
        private readonly VoiceChannelHandler _voiceChannelHandler;
        private readonly SlashCommandHandler _slashCommandHandler;

        public DiscordBot(IConfiguration configuration,
            GuildConfigService guildConfigService,
            SlashCommandHandler slashCommandHandler,
            VoiceChannelHandler voiceChannelHandler)
        {
            _configuration = configuration;
            _guildConfigService = guildConfigService;
            _slashCommandHandler = slashCommandHandler;
            _voiceChannelHandler = voiceChannelHandler;
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildVoiceStates,
                LogLevel = LogSeverity.Info,
            });
        }

        public async Task Start()
        {
            _source = new CancellationTokenSource();
            try
            {
                IsRunning = true;
                await RunAsync(_source.Token);
            }
            catch (OperationCanceledException ocex)
            {
                if (!_source.IsCancellationRequested)
                    Log.Error(ocex, ocex.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DiscordBot Exception:");
            }
            finally
            {
                IsRunning = false;
                Stop();
            }
        }
        public void Stop()
        {
            if (_source != null)
                _source.Cancel();
        }
        public void Stop(int delay)
        {
            if (_source != null)
                _source.CancelAfter(delay);
        }
        public void Stop(TimeSpan delay)
        {
            if (_source != null)
                _source.CancelAfter(delay);
        }
        public async Task Restart()
        {
            Stop();
            await Start();
        }

        private async Task RunAsync(CancellationToken token = default)
        {
            var discordToken = _configuration["Discord:token"];

            if (string.IsNullOrWhiteSpace(discordToken))
            {
                throw new ArgumentNullException("Discord token cannot be empty or contain whitespace.");
            }

            try
            {
                await (_client as BaseSocketClient).LoginAsync(TokenType.Bot, discordToken);
                _client.Log += LogHandler.OnLogAsync;
                _client.UserVoiceStateUpdated += _voiceChannelHandler.HandleVoiceStateUpdated;
                _client.SlashCommandExecuted += _slashCommandHandler.OnSlashCommand;
                _client.Ready += OnReady;
                _client.JoinedGuild += OnJoinedGuild;
                await _client.StartAsync();

                await Task.Delay(-1, token);
            }
            finally
            {
                if (_client != null)
                {
                    await _client.LogoutAsync();
                    _client.Dispose();
                }
            }
        }

        private Task OnJoinedGuild(SocketGuild guild)
        {
            SendWelcomeMessage(guild);
            CreateGuildConfigFile(guild.Id);
            return Task.CompletedTask;
        }

        private Task OnReady()
        {
            foreach (var guild in _client.Guilds)
            {
                Log.Information($"Check for {guild.Id} guild.");

                CreateGuildSlashCommands(guild);
                CheckBotVoiceChannelsAccess(guild);
            }
            return Task.CompletedTask;
        }

        private void CheckBotVoiceChannelsAccess(SocketGuild guild)
        {
            var config = _guildConfigService.GetGuildConfig(guild.Id);
            if (config == null) SendWelcomeMessage(guild);
            CreateGuildConfigFile(guild.Id);
        }

        private void CreateGuildConfigFile(ulong id)
        {
            _guildConfigService.CreateGuildConfig(id);
        }

        private async void SendWelcomeMessage(SocketGuild guild)
        {
            var welcomeEmbed = GetWelcomeEmbed();
            // SEND MSG TO OWNER
            //try
            //{
            //    guild.Owner.SendMessageAsync(null, embed: welcomeEmbed);
            //}
            //catch (HttpException ex)
            //{
            //if (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            //{
            await guild.SystemChannel.SendMessageAsync(null, embed: welcomeEmbed); //TODO(Hantick) Does not work always (missing permissions on OnJoinedGuild)
            //}
            //}
        }

        private Embed GetWelcomeEmbed()
        {
            var builder = new EmbedBuilder()
            .WithTitle("Time to say hi!")
            .WithDescription("Hi! Sorry for writing here, but guild owner doesn't accept messages from server users 😢\nThank you for adding me to this server, I'll do my best to provide the best dynamic voice channel experience you ever met!")
            //.WithUrl("https://discordapp.com")
            .WithColor(new Color(0x19C2B1))
            .WithTimestamp(DateTimeOffset.FromUnixTimeMilliseconds(1642353769955))
            .WithFooter(footer =>
            {
                footer
                    .WithText("Welcome message")
                    .WithIconUrl("https://cdn.discordapp.com/embed/avatars/0.png");
            })
            .WithThumbnailUrl("https://cdn.discordapp.com/embed/avatars/0.png")
            .AddField("How to configure <:thonkang:219069250692841473>", "To add channels for dynamic watch simply use `/add-channel` and bla bla...")
            .AddField("Contribution", "DynamicVoiceChannelBOT is an open source project hosted on github")
            .AddField("<:thonkang:219069250692841473>", "these last two");

            var embed = builder.Build();
            return embed;
        }

        private async void CreateGuildSlashCommands(SocketGuild guild)
        {
            var guildCommands = new List<SlashCommandBuilder>();
            guildCommands.Add(
                new SlashCommandBuilder()
                .WithName(SlashCommandHandler.COMMAND_CHANNELS)
                .WithDescription("Retrieves a list of managed voice channels for dynamic creating."));

            guildCommands.Add(
                new SlashCommandBuilder()
                .WithName(SlashCommandHandler.COMMAND_ADD_CHANNEL)
                .WithDescription("Adds a channel to a list of managed voice channels for dynamic creating.")
                .AddOption("channel", ApplicationCommandOptionType.Channel, "Channel you want to add to the managed voice channels list.", true));
            try
            {
                foreach (var guildCommand in guildCommands)
                {
                    await guild.CreateApplicationCommandAsync(guildCommand.Build());
                }
            }
            catch (HttpException exception)
            {
                Log.Warning(exception.Message);
            }
        }

    }
}