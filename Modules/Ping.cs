using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace RuneBotNET.Commands {

    public class Ping : ModuleBase<SocketCommandContext> {

        private readonly string[] responses = {
            "Hey!",
            "Pong!",
            "Polo!",
            "Aww... I wanted to say ping!",
            "I'm up! I'm up...",
            "Yes?",
            "You have my attention, outlander.",
            "...",
            "Not dignifying that with a response.",
            "Ping yourself!",
            "Reporting for duty!",
            "One ping only, Vasily.",
            "That tickles!",
            "At your service!",
            "O rly?",
            "Oooh! ( ͡° ͜ʖ ͡°)",
            "Да, Comrade?",
            "UwU",
            "You ever wonder why we're here?"
        };

        [Command("ping")]
        [Alias("poke")]
        [Summary("Pings the bot to make sure it's paying attention.")]
        public async Task ExecuteAsync() {

            Random random = new Random();
            int choice = random.Next(0, responses.Length);

            await Context.User.SendMessageAsync(responses[choice]);
        }
    }
}
