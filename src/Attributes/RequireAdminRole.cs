using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace OSISDiscordAssistant.Attributes
{
    /// <summary>
    /// Checks whether the command invoker has either the Moderator, Panitia, Inti OSIS, Administrator, or Service Administrator role.
    /// If the user has none of them, then it will check for a role that the member has with administrator permission enabled.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequireAdminRole : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            bool isAdmin = false;

            isAdmin = ctx.Member.Roles.Any(x => x.Name == "Service Administrator") || ctx.Member.Roles.Any(x => x.Name == "Administrator")
                || ctx.Member.Roles.Any(x => x.Name == "Inti OSIS" || ctx.Member.Roles.Any(x => x.Name == "Panitia")
                || ctx.Member.Roles.Any(x => x.Name == "Moderator"));

            if (!isAdmin)
            {
                isAdmin = ctx.Member.Permissions.HasPermission(Permissions.Administrator);
            }          

            return Task.FromResult(isAdmin);
        }
    }
}