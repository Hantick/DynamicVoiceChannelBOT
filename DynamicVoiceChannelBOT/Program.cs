
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
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

                })
                .UseSerilog()
                .Build();


            Log.Logger.Information("Application starting...");
        }

        static void BuildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        }
    }
}
