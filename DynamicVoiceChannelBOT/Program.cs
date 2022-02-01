
using Discord.WebSocket;
using DynamicVoiceChannelBOT.Handlers;
using DynamicVoiceChannelBOT.Services;
using DynamicVoiceChannelBOT.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace DynamicVoiceChannelBOT
{
    public static class Program
    {
        private static void Main()
        {
            Console.Title = "Dynamic Voice Channels Bot";

            var builder = new ConfigurationBuilder();
            BuildConfig(builder);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Build())
                .Enrich.FromLogContext()
                .WriteTo.Console(
                LogEventLevel.Verbose,
                "{Timestamp:HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}")
                .CreateLogger();

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped<CancellationTokenSource>();
                    services.AddSingleton<DiscordBot>();
                    services.AddTransient<IDataStorage, JsonStorage>();
                    services.AddScoped<GuildConfigService>();
                    services.AddScoped<SlashCommandHandler>();
                    services.AddScoped<VoiceChannelHandler>();
                })
                .UseSerilog()
                .Build();


            Log.Logger.Information("Application starting...");

            var discordBot = ActivatorUtilities.CreateInstance<DiscordBot>(host.Services);
            _ = discordBot.Start();
            HandleInput(discordBot);
        }

        private static void BuildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        }

        private static void HandleInput(DiscordBot bot)
        {
            while (true)
            {
                switch (Console.ReadKey().KeyChar)
                {
                    case 'q':
                        {
                            ClearCurrentConsoleLine();
                            Console.WriteLine("Exit program (q)?");
                            var choice2 = Console.ReadKey().KeyChar;
                            switch (choice2)
                            {
                                case 'q':
                                    ClearCurrentConsoleLine();
                                    bot.Stop();
                                    break;
                                default:
                                    ClearCurrentConsoleLine();
                                    break;
                            }

                            break;
                        }
                    default:
                        ClearCurrentConsoleLine();
                        break;
                }
            }
        }
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
