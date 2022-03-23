using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace OSISDiscordAssistant.Attributes
{
    /// <summary>
    /// Checks whether the command invoker has the Service Administrator role.
    /// </summary>
    public class RequireServiceAdminRole : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return Task.FromResult(ctx.Member.Roles.Any(x => x.Name == "Service Administrator"));
        }
    }
}
