using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RuneBotNET.Commands {

    public class Help : ModuleBase<SocketCommandContext> {

        public CommandService _service { get; set; }

        [Command("help")]
        [Summary("Show this list of all of RuneBot's commands.")]
        public async Task ExecuteAsync() {

            var embed = new EmbedBuilder {

                Title = ":information_source: RuneBot Help",
                Color = new Color(212, 175, 55),
                Description = "Hello there! ( ͡° ͜ʖ ͡°)",
                ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl()
            };

            foreach(var module in _service.Modules) {

                string commandName = null;
                foreach(var command in module.Commands) {

                    var result = await command.CheckPreconditionsAsync(Context);
                    if(result.IsSuccess) {

                        string aliasMessage = null;

                        if(command.Aliases.Count > 1) {

                            aliasMessage += "[";

                            // Index starts at 1 because the first element of the list
                            // is the command itself, not an alias
                            for(int i = 1; i < command.Aliases.Count; i++) {

                                aliasMessage += command.Aliases[i] + ((i == command.Aliases.Count - 1) ? "]" : "|");
                            }
                        }
                        
                        commandName += $"{RuneBot._config["prefix"]}{command.Aliases.First()} {aliasMessage}";
                    }

                    if(!string.IsNullOrWhiteSpace(commandName)) {

                        embed.AddField(x => {

                            x.Name = commandName;
                            x.Value = command.Summary;
                            x.IsInline = false;
                        });
                    }
                }
            }

            await Context.User.SendMessageAsync(embed: embed.Build());
        }
    }
}
