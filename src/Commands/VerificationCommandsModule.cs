using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;
using OSISDiscordAssistant.Models;
using OSISDiscordAssistant.Attributes;
using OSISDiscordAssistant.Utilities;
using OSISDiscordAssistant.Services;

namespace OSISDiscordAssistant.Commands
{
    class VerificationCommandsModule : BaseCommandModule
    {    
        [RequireMainGuild, RequireAdminRole]
        [Command("overify")]
        public async Task OverifyAsync(CommandContext ctx, DiscordMember member)
        {
            // Checks whether the invoker is manually verifying themself.
            if (await ClientUtilities.CheckSelfTargeting(member, ctx))
            {
                return;
            }

            try
            {
                // Grants the access role to the targeted user.
                await member.GrantRoleAsync(ctx.Guild.GetRole(SharedData.AccessRoleId));

                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} {member.Mention} has been given the access role.");
            }

            catch
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} An error occured. Have you tried to use the command correctly?");
            }
        }

        [RequireMainGuild, RequireAdminRole]
        [Command("overify")]
        public async Task OverifyWithNameAsync(CommandContext ctx, DiscordMember member, params string[] displayName)
        {
            // Checks whether the invoker is manually verifying themself.
            if (await ClientUtilities.CheckSelfTargeting(member, ctx))
            {
                return;
            }

            try
            {
                // Grants the access role to the targeted user.
                await member.GrantRoleAsync(ctx.Guild.GetRole(SharedData.AccessRoleId));
                await member.ModifyAsync(setName => setName.Nickname = string.Join(" ", displayName));

                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} {member.Mention} has been given the access role and assigned a new nickname ({string.Join(" ", displayName)}).");
            }

            catch
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} An error occured. Have you tried to use the command correctly?");
            }
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [RequireMainGuild, RequireAdminRole]
        [Command("overify")]
        public async Task OverifyHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} !overify [USER MENTION or USER ID] [NEW NICKNAME (optional)]");
        }
    }
}
