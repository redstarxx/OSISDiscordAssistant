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
            if (ctx.Member == member)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You cannot use this command on yourself.");

                return;
            }

            await member.GrantRoleAsync(ctx.Guild.GetRole(SharedData.AccessRoleId));
            await member.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} Kamu telah diverifikasi secara manual oleh {ctx.Member.Mention}! Sekarang kamu bisa mengakses channel internal server {ctx.Guild.Name} dan mendapatkan role seksimu di <#{SharedData.RolesChannelId}>.");

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} {member.Mention} has been granted the access role.");

            await CheckPendingVerificationRequest(ctx, member);
        }

        [RequireMainGuild, RequireAdminRole]
        [Command("overify")]
        public async Task OverifyWithNameAsync(CommandContext ctx, DiscordMember member, [RemainingText] string displayName)
        {
            if (ctx.Member == member)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You cannot use this command on yourself.");

                return;
            }

            await member.GrantRoleAsync(ctx.Guild.GetRole(SharedData.AccessRoleId));
            await member.ModifyAsync(setName => setName.Nickname = displayName);

            await member.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} Kamu telah diverifikasi secara manual oleh {ctx.Member.Mention}! Sekarang kamu bisa mengakses channel internal server {ctx.Guild.Name} dan mendapatkan role seksimu di <#{SharedData.RolesChannelId}>.");
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} {member.Mention} has been granted the access role and assigned a new nickname ({displayName}).");

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
