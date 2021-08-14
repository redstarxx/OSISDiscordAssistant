using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace OSISDiscordAssistant.Attributes
{
    /// <summary>
    /// Checks whether the command invoker has the OSIS role.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequireAccessRole : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            bool hasAccessRole = ctx.Member.Roles.Any(x => x.Name == "OSIS");

            return Task.FromResult(hasAccessRole);
        }
    }
}