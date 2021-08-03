using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace discordbot.Attributes
{
    public class RequireServiceAdminRole : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            bool isServiceAdmin = ctx.Member.Roles.Any(x => x.Name == "Service Administrator");

            return Task.FromResult(isServiceAdmin);
        }
    }
}
