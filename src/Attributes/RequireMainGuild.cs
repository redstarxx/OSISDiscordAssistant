using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace discordbot.Attributes
{
    /// <summary>
    /// Checks whether the command is executed in the OSIS main guild.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequireMainGuild : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            ulong mainGuildId = 814445508583358494;

            return Task.FromResult(ctx.Guild.Id == mainGuildId);
        }
    }
}