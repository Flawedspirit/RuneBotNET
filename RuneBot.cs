using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RuneBotNET.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RuneBotNET {

    class RuneBot {

        public static readonly IConfiguration _config;
        public static List<DateTimeOffset> _cooldownTime;
        public static List<SocketGuildUser> _cooldownUser;

        private DiscordSocketClient _client;
        private static string _logLevel;

        static void Main(string[] args = null) {

            if (args.Count() != 0) _logLevel = args[0];

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/runebot.log", rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();

            new RuneBot().MainAsyncTask().GetAwaiter().GetResult();
        }

        static RuneBot() {

            // Create bot configuration object
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json");
            _config = configBuilder.Build();

            // Create cooldown system
            _cooldownTime = new List<DateTimeOffset>();
            _cooldownUser = new List<SocketGuildUser>();
        }

        public async Task MainAsyncTask() {

            using var services = ConfigureServices();
            var client = services.GetRequiredService<DiscordSocketClient>();
            _client = client;

            // Set up logging service
            services.GetRequiredService<LoggingService>();

            // Read secret token from environment variables
            // to avoid hard-coding it somewhere.
            await _client.LoginAsync(TokenType.Bot, _config["token"]);
            await _client.StartAsync();

            // Load the logic needed to register commands
            await services.GetRequiredService<BotCommandService>().InitializeAsync();

            //Run first-time help on joining
            _client.JoinedGuild += IntroduceMyselfAsync;

            // Keep main thread alive until the heat death of the Universe
            // or until the bot crashes/is killed. Whichever.
            await Task.Delay(Timeout.Infinite);
        }

        private ServiceProvider ConfigureServices() {

            var services = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<BotCommandService>()
                .AddSingleton<LoggingService>()
                .AddLogging(configure => configure.AddSerilog());

            if (!string.IsNullOrEmpty(_logLevel)) {

                switch (_logLevel.ToLower()) {

                    case "debug": {
                            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);
                            break;
                        }
                    case "info": {
                            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information);
                            break;
                        }
                    case "warn": {
                            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Warning);
                            break;
                        }
                    default: {
                            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Error);
                            break;
                        }
                }
            } else {

                services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information);
            }

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }

        private async Task IntroduceMyselfAsync(SocketGuild guild) {

            var embed = new EmbedBuilder {

                Title = ":books: Getting Started with RuneBot",
                Color = new Color(212, 175, 55),
                Description = $@"Greetings! I am RuneBot, at your service.
                    Here is some information to get you started:

                    My command prefix is `{_config["prefix"]}`.
                    To see a list of all my commands, type **{_config["prefix"]}help**.",
                Timestamp = DateTime.Now,
                Footer = new EmbedFooterBuilder {
                    Text = "A Discord bot by Flawedspirit!",
                    IconUrl = guild.CurrentUser.GetAvatarUrl()
                }
            };

            await guild.DefaultChannel.SendMessageAsync(embed: embed.Build());
        }
    }
}
