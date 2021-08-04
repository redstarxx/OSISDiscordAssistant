using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;
using discordbot.Attributes;

namespace discordbot.Commands
{
    class BotAdministrationCommands : BaseCommandModule
    {
        [RequireServiceAdminRole]
        [Command("reconnect")]
        public async Task ReconnectAsync(CommandContext ctx)
        {
            Stopwatch stopwatch = new Stopwatch();

            await ctx.Channel.SendMessageAsync($"Reconnecting...").ConfigureAwait(false);
            Bot.Client.Logger.LogWarning(Bot.LogEvent, $"{ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.User.Id}) initiated a reconnect command.", DateTime.UtcNow.AddHours(7));

            stopwatch.Start();
            await ctx.Client.ReconnectAsync(true);

            Bot.StartStatusUpdater();

            stopwatch.Stop();

            await ctx.Channel.SendMessageAsync($"Successfully reconnected to the gateway with a new session. It took {stopwatch.ElapsedMilliseconds} ms.").ConfigureAwait(false);
        }

        [RequireServiceAdminRole]
        [Command("kill")]
        public async Task KillAsync(CommandContext ctx)
        {
            Bot.Client.Logger.LogWarning(Bot.LogEvent, $"{ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.User.Id}) initiated a kill command.", DateTime.UtcNow.AddHours(7));
            await ctx.Channel.SendMessageAsync($"Disconnecting from the gateway...").ConfigureAwait(false);
            Thread.Sleep(TimeSpan.FromSeconds(1));

            await ctx.Client.DisconnectAsync();

            Environment.Exit(0);
        }
    }
}
