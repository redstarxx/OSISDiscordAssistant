using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;

namespace discordbot.Commands
{
    class BotAdministrationCommands : BaseCommandModule
    {
        [Command("reconnect")]
        public async Task ReconnectAsync(CommandContext ctx)
        {
            bool isServiceAdmin = ClientUtilities.CheckServiceAdminRole(ctx);

            if (!isServiceAdmin)
            {
                string errorMessage = $"{Formatter.Bold("[ERROR]")} You must have the {Formatter.InlineCode("Service Administrator")} role to use this command.";

                await ctx.Channel.SendMessageAsync(errorMessage).ConfigureAwait(false);

                return;
            }

            Stopwatch stopwatch = new Stopwatch();

            await ctx.Channel.SendMessageAsync($"Reconnecting...").ConfigureAwait(false);
            Bot.Client.Logger.LogWarning(Bot.LogEvent, $"{ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.User.Id}) initiated a reconnect command.", DateTime.UtcNow.AddHours(7));

            stopwatch.Start();
            await ctx.Client.ReconnectAsync(true);

            stopwatch.Stop();

            await ctx.Channel.SendMessageAsync($"Successfully reconnected to the gateway without starting a new session. It took {stopwatch.ElapsedMilliseconds} ms.").ConfigureAwait(false);
        }

        [Command("kill")]
        public async Task KillAsync(CommandContext ctx)
        {
            bool isServiceAdmin = ClientUtilities.CheckServiceAdminRole(ctx);

            if (!isServiceAdmin)
            {
                string errorMessage = $"{Formatter.Bold("[ERROR]")} You must have the {Formatter.InlineCode("Service Administrator")} role to use this command.";

                await ctx.Channel.SendMessageAsync(errorMessage).ConfigureAwait(false);

                return;
            }

            Bot.Client.Logger.LogWarning(Bot.LogEvent, $"{ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.User.Id}) initiated a kill command.", DateTime.UtcNow.AddHours(7));
            await ctx.Channel.SendMessageAsync($"Disconnecting from the gateway...").ConfigureAwait(false);
            Thread.Sleep(TimeSpan.FromSeconds(1));

            await ctx.Client.DisconnectAsync();
        }
    }
}
