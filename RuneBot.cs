using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RuneBotNET.Services;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RuneBotNET {

    class RuneBot {

        public static readonly IConfiguration config;
        private DiscordSocketClient client;
        private static string logLevel;

        static void Main(string[] args = null) {

            if (args.Count() != 0) logLevel = args[0];

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
            config = configBuilder.Build();
        }

        public async Task MainAsyncTask() {

            using var services = ConfigureServices();
            var _client = services.GetRequiredService<DiscordSocketClient>();
            client = _client;

            // Set up logging service
            services.GetRequiredService<LoggingService>();

            // Read secret token from environment variables
            // to avoid hard-coding it somewhere.
            await client.LoginAsync(TokenType.Bot, config["token"]);
            await client.StartAsync();

            // Load the logic needed to register commands
            await services.GetRequiredService<BotCommandService>().InitializeAsync();

            //Run first-time help on joining
            client.JoinedGuild += IntroduceMyselfAsync;

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

            if (!string.IsNullOrEmpty(logLevel)) {

                switch (logLevel.ToLower()) {

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

                    My command prefix is `{config["prefix"]}`.
                    To see a list of all my commands, type **{config["prefix"]}help**.",
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
