using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using OSISDiscordAssistant.Models;
using OSISDiscordAssistant.Attributes;
using OSISDiscordAssistant.Utilities;
using OSISDiscordAssistant.Services;
using Microsoft.Extensions.Logging;

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
                await member.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} You have been manually verified by {ctx.Member.Mention}! You may now access the internal channels of {ctx.Guild.Name} and begin your interaction!");

                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} {member.Mention} has been granted the access role.");

                using (var db = new VerificationContext())
                {
                    var pendingVerification = db.Verifications.SingleOrDefault(x => x.UserId == member.Id);

                    if (pendingVerification != null)
                    {
                        var requestEmbed = await ctx.Guild.GetChannel(SharedData.VerificationRequestsProcessingChannelId).GetMessageAsync(pendingVerification.VerificationEmbedId);

                        foreach (var embed in requestEmbed.Embeds)
                        {
                            DiscordEmbed updatedEmbed = null;

                            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder(embed)
                            {
                                Title = $"{embed.Title.Replace(" | PENDING", " | ACCEPTED")}",
                                Description = $"{embed.Description.Replace("PENDING.", $"ACCEPTED (overrided by {ctx.Member.Mention}, at {Formatter.Timestamp(DateTime.Now, TimestampFormat.LongDateTime)}).")}"
                            };

                            updatedEmbed = embedBuilder.Build();

                            await requestEmbed.ModifyAsync(x => x.WithEmbed(updatedEmbed));                          

                            db.Remove(pendingVerification);
                            await db.SaveChangesAsync();

                            break;
                        }                       
                    }
                }
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
