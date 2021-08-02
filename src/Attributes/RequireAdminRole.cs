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
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequireAdminRole : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            bool hasServiceAdminRole = ctx.Member.Roles.Any(x => x.Name == "Service Administrator");

            bool hasAdminRole = ctx.Member.Roles.Any(x => x.Name == "Administrator");

            bool hasCoreCouncilRole = ctx.Member.Roles.Any(x => x.Name == "Inti OSIS");

            bool permissionGranted = false;

            if (!hasServiceAdminRole)
            {
                if (!hasAdminRole)
                {
                    if (!hasCoreCouncilRole)
                    {
                        permissionGranted = false;
                    }

                    else
                    {
                        permissionGranted = true;
                    }
                }

                else
                {
                    permissionGranted = true;
                }
            }

            else
            {
                permissionGranted = true;
            }

            return Task.FromResult(permissionGranted);
        }
    }
}