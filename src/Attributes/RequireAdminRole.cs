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
    /// Checks whether the command invoker has either the Inti OSIS, Administrator or Service Administrator role.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequireAdminRole : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            bool isAdmin = ctx.Member.Roles.Any(x => x.Name == "Service Administrator") || ctx.Member.Roles.Any(x => x.Name == "Administrator") 
                || ctx.Member.Roles.Any(x => x.Name == "Inti OSIS" || ctx.Member.Roles.Any(x => x.Name == "Panitia") 
                || ctx.Member.Roles.Any(x => x.Name == "Moderator"));

            return Task.FromResult(isAdmin);
        }
    }
}