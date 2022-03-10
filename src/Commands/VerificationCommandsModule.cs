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

namespace OSISDiscordAssistant.Commands
{
    class VerificationCommandsModule : BaseCommandModule
    {
        private readonly VerificationContext _verificationContext;

        public VerificationCommandsModule(VerificationContext verificationContext)
        {
            _verificationContext = verificationContext;
        }

        [RequireMainGuild, RequireAdminRole]
        [Command("overify")]
        public async Task OverifyAsync(CommandContext ctx, DiscordMember member)
        {
            if (await ClientUtilities.CheckSelfTargeting(member, ctx))
            {
                return;
            }

            await member.GrantRoleAsync(ctx.Guild.GetRole(SharedData.AccessRoleId));
            await member.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} You have been manually verified by {ctx.Member.Mention}! You may now access the internal channels of {ctx.Guild.Name} and begin your interaction!");

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} {member.Mention} has been granted the access role.");

            await CheckPendingVerificationRequest(ctx, member);
        }

        [RequireMainGuild, RequireAdminRole]
        [Command("overify")]
        public async Task OverifyWithNameAsync(CommandContext ctx, DiscordMember member, [RemainingText] string displayName)
        {
            if (await ClientUtilities.CheckSelfTargeting(member, ctx))
            {
                return;
            }

            await member.GrantRoleAsync(ctx.Guild.GetRole(SharedData.AccessRoleId));
            await member.ModifyAsync(setName => setName.Nickname = displayName);

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} {member.Mention} has been given the access role and assigned a new nickname ({displayName}).");

            await CheckPendingVerificationRequest(ctx, member);
        }

        private async Task<Task> CheckPendingVerificationRequest(CommandContext ctx, DiscordMember member)
        {
            var pendingVerification = _verificationContext.Verifications.SingleOrDefault(x => x.UserId == member.Id);

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

                    _verificationContext.Remove(pendingVerification);
                    await _verificationContext.SaveChangesAsync();

                    break;
                }
            }

            return Task.CompletedTask;
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [RequireMainGuild, RequireAdminRole]
        [Command("overify")]
        public async Task OverifyHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis overify [MENTION or USER ID] [NEW NICKNAME (optional)]");
        }
    }
}
