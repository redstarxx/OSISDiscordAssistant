using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using OSISDiscordAssistant.Constants;

namespace OSISDiscordAssistant.Attributes
{
    /// <summary>
    /// Checks whether the command is executed in the OSIS main guild.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequireMainGuild : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return Task.FromResult(ctx.Guild.Id == StringConstants.MainGuildId);
        }
    }
}