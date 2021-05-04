using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace RuneBotNET.Services {

    public class LoggingService {

        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly ILogger _logger;

        public LoggingService(IServiceProvider services) {

            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _logger = services.GetRequiredService<ILogger<LoggingService>>();

            _client.Ready += OnReadyAsync;
            _client.Log += OnLogAsync;
            _commands.Log += OnLogAsync;

            _logger.LogInformation("Logging service started.");
        }

        private Task OnReadyAsync() {

            _logger.LogInformation($"Connected as -> {_client.CurrentUser}.");
            _logger.LogInformation($"RuneBot running on {_client.Guilds.Count} server(s).");
            return Task.CompletedTask;
        }

        private Task OnLogAsync(LogMessage message) {

            string logMessage = $"{message.Exception?.ToString() ?? message.Message}";

            switch (message.Severity.ToString()) {

                case "Debug": {
                        _logger.LogDebug(logMessage);
                        break;
                    }
                case "Verbose": {
                        _logger.LogInformation(logMessage);
                        break;
                    }
                case "Info": {
                        _logger.LogInformation(logMessage);
                        break;
                    }
                case "Warning": {
                        _logger.LogWarning(logMessage);
                        break;
                    }
                case "Error": {
                        _logger.LogError(logMessage);
                        break;
                    }
                case "Critical": {
                        _logger.LogCritical(logMessage);
                        break;
                    }
            }

            return Task.CompletedTask;
        }
    }
}
