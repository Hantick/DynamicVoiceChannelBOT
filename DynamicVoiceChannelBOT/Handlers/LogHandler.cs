using Discord;
using Serilog;

namespace DynamicVoiceChannelBOT.Handlers
{
    public static class LogHandler
    {
        public static Task OnLogAsync(LogMessage msg)
        {
            string logText = $"DISCORD: {msg.Exception?.ToString() ?? msg.Message}";
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    {
                        Log.Fatal(logText);
                        break;
                    }
                case LogSeverity.Warning:
                    {
                        Log.Warning(logText);
                        break;
                    }
                case LogSeverity.Verbose:
                case LogSeverity.Info:
                    {
                        Log.Information(logText);
                        break;
                    }
                case LogSeverity.Debug:
                    {
                        Log.Debug(logText);
                        break;
                    }
                case LogSeverity.Error:
                    {
                        Log.Error(logText);
                        break;
                    }
            }

            return Task.CompletedTask;
        }
    }
}
