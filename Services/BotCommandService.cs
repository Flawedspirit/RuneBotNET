using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace RuneBotNET.Services {

    public class BotCommandService {

        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;

        public BotCommandService(IServiceProvider services, CommandService commands, DiscordSocketClient client) {

            _commands = commands;
            _client = client;
            _logger = services.GetRequiredService<ILogger<BotCommandService>>();
            _services = services;

            _client.MessageReceived += MessageReceivedAsync;
            _commands.CommandExecuted += CommandExecutedAsync;
        }

        public async Task InitializeAsync() {

            // Register modules that are public and inherit ModuleBase<T>
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage) {

            // Ignore messages sent by the system
            if (!(rawMessage is SocketUserMessage message)) return;

            // Position index of the argument in the input string
            int argPos = 0;

            // Determine if the message is a command based on prefix
            // Also make sure that other bots' messages don't trigger this bot
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            var context = new SocketCommandContext(_client, message);

            // Check if user has sent a command within the configured rate limit
            // If so, tell the user when the cooldown ends and bail
            if(RuneBot._cooldownUser.Contains(context.User as SocketGuildUser)) {

                // if the user used a command, add the configured rateLimit,
                // and see if it's greater than the current time
                if(RuneBot._cooldownTime[RuneBot._cooldownUser.IndexOf(context.Message.Author as SocketGuildUser)]
                .AddSeconds(Convert.ToDouble(RuneBot._config["rateLimit"])) >= DateTimeOffset.Now) {

                    // It is not; let the user know how much time is left and exit
                    double secondsLeft = (RuneBot._cooldownTime[RuneBot._cooldownUser.IndexOf(context.Message.Author as SocketGuildUser)]
                    .AddSeconds(Convert.ToDouble(RuneBot._config["rateLimit"])) - DateTimeOffset.Now).TotalSeconds;

                    await context.User.SendMessageAsync($"Please wait {secondsLeft:N1} second(s) before sending another command.");
                    return;
                } else {

                    // It is, and the user can execute a command. Set the last executed time to right now
                    RuneBot._cooldownTime[RuneBot._cooldownUser.IndexOf(context.Message.Author as SocketGuildUser)] = DateTimeOffset.Now;
                }
            } else {

                // This user has never executed a command; add them to the list
                RuneBot._cooldownTime.Add(DateTimeOffset.Now);
                RuneBot._cooldownUser.Add(context.User as SocketGuildUser);
            }            

            // Actually execute the command
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result) {

            // Ignore messages that we don't care about
            // Namely, whether a command does not exist, or if it executed successfully
            if (!command.IsSpecified) return;
            if (result.IsSuccess) return;

            // If the command did not execute, fall down into this error and die in a fire
            _logger.LogError($"Command -> \"{context.Message}\": {result}");

            await context.Channel.SendMessageAsync(RuneBot._config["errorMessage"]);
        }
    }
}
