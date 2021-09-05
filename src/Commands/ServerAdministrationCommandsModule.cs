using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using OSISDiscordAssistant.Attributes;
using OSISDiscordAssistant.Utilities;

namespace OSISDiscordAssistant.Commands
{
    class ServerAdministrationCommandsModule : BaseCommandModule
    {
        [RequireAdminRole]
        [Command("mute")]
        public async Task MuteAsync(CommandContext ctx, DiscordMember member, [RemainingText] string muteReason)
        {
            // Checks whether the invoker is muting themself.
            if (member.Id == ctx.Member.Id)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You cannot mute yourself.").ConfigureAwait(false);

                return;
            }

            // Checks whether the command is executed with a reason.
            if (muteReason is null)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You must specify a reason.").ConfigureAwait(false);

                return;
            }

            var mutedRole = ctx.Guild.Roles.SingleOrDefault(x => x.Value.Name == "Muted").Value.Id;
            await member.GrantRoleAsync(ctx.Guild.GetRole(mutedRole));
            string mutingUser = ctx.Member.DisplayName;

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[MUTED]")} {member.Mention} has been muted by {ctx.Member.Mention}. Reason: {muteReason}").ConfigureAwait(false);
        }

        [RequireAdminRole]
        [Command("unmute")]
        public async Task UnmuteAsync(CommandContext ctx, DiscordMember member)
        {
            // Checks whether the invoker is unmuting themself.
            if (member.Id == ctx.Member.Id)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You cannot unmute yourself.").ConfigureAwait(false);

                return;
            }

            var mutedRole = ctx.Guild.Roles.SingleOrDefault(x => x.Value.Name == "Muted").Value.Id;
            await member.RevokeRoleAsync(ctx.Guild.GetRole(mutedRole));
            string unmutingUser = ctx.Member.DisplayName;

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[UNMUTED]")} {member.Mention} has been unmuted by {ctx.Member.Mention}.").ConfigureAwait(false);
        }

        [RequireAdminRole]
        [Command("kick")]
        public async Task KickAsync(CommandContext ctx, DiscordMember member, [RemainingText] string kickReason)
        {
            //Check if the invoker is trying to kick themself.
            if (member.Id == ctx.Member.Id)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Are you trying to kill yourself?");

                return;
            }

            // Checks whether the command is executed with a reason.
            if (kickReason is null)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You must specify a reason.").ConfigureAwait(false);

                return;
            }

            try
            {
                await member.RemoveAsync(kickReason);

                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[KICKED]")} {member.Mention} has been kicked from this server by {ctx.Member.Mention}. Reason: {kickReason}").ConfigureAwait(false);
            }

            catch
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} An error occured. The member may not be in this server.").ConfigureAwait(false);
            }
        }

        [RequireAdminRole]
        [Command("setname")]
        public async Task SetNameAsync(CommandContext ctx, DiscordMember member, [RemainingText] string newNickname)
        {
            if (ctx.User.Id != member.Id)
            {
                bool isAdmin = ClientUtilities.CheckAdminPermissions(ctx);

                if (!isAdmin)
                {
                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} If you are not an administrator, you cannot rename someone else's nickname!").ConfigureAwait(false);

                    return;
                }
            }

            // Checks whether the command is executed with a reason.
            if (newNickname is null)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You must specify a new name.").ConfigureAwait(false);

                return;
            }

            try
            {
                string previousNickname = member.DisplayName;
                await member.ModifyAsync(x => x.Nickname = newNickname);

                await ctx.Channel.SendMessageAsync($"{member.Mention} {previousNickname}'s server username has been changed to {newNickname} by {ctx.Member.Mention}.").ConfigureAwait(false);
            }

            catch
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} An error occured. The member may not be in this server.").ConfigureAwait(false);
            }
        }

        [RequireAdminRole]
        [Command("announce")]
        public async Task AnnounceAsync(CommandContext ctx, DiscordChannel channel, DiscordRole role, [RemainingText] string announceMessage)
        {
            try
            {
                if (announceMessage is null)
                {
                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Announcement message cannot be empty!");

                    return;
                }

                await channel.SendMessageAsync($"{Formatter.Bold("[ANNOUNCEMENT]")} {role.Mention} {announceMessage}");
            }

            catch (Exception ex)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} An error occured. Are you trying to send the message to a channel which the bot does not have access?\nException details: {ex.Message}");
            }
        }

        [RequireAdminRole]
        [Command("ban")]
        public async Task BanAsync(CommandContext ctx, DiscordMember member, [RemainingText] string reason)
        {
            if (member.Id == ctx.Member.Id)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Are you trying to kill yourself?");

                return;
            }

            if (reason is null)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You must specify a reason.");

                return;
            }

            await member.BanAsync(0, reason);

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[BANNED]")} {member.Username}#{member.Discriminator} has been banned by {ctx.Member.Mention}. Reason: {reason}");
        }

        [RequireAdminRole]
        [Command("unban")]
        public async Task UnbanAsync(CommandContext ctx, DiscordUser member, [RemainingText] string reason)
        {
            if (member.Id == ctx.Member.Id)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You cannot unban yourself!");

                return;
            }

            DiscordGuild guild = ctx.Member.Guild;

            reason = reason is null ? reason = "N/A" : reason;

            await member.UnbanAsync(guild, reason);

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[UNBANNED]")} {member.Username}#{member.Discriminator} has been unbanned by {ctx.Member.Mention}. Reason: {reason}");
        }

        [Command("sendinfoembed")]
        public async Task InfoEmbedAsync(CommandContext ctx, ulong targetChannelId)
        {
            DiscordChannel targetChannel = await Bot.Client.GetShard(ctx.Guild.Id).GetChannelAsync(targetChannelId);

            var informationChannelEmbed = new DiscordEmbedBuilder
            {
                Title = "OSIS Sekolah Djuwita Batam - Discord Server",
                Description = $"Selamat datang di server Discord {Formatter.Bold("OSIS Sekolah Djuwita Batam!")} Akses ke server ini hanya diperbolehkan untuk anggota OSIS aktif.\n\n" +
                $"Untuk meminta verifikasi akses, ketik {Formatter.InlineCode("!requestverify")} di <#832275177160048711>. Inti OSIS akan memproses permintaan verifikasimu secepatnya. " +
                $"Sebagai alternatif, anggota Inti OSIS dapat langsung memverifikasi aksesmu dengan {Formatter.InlineCode("!overify")}.",
                Color = DiscordColor.MidnightBlue
            };

            string inviteLink = $"{Formatter.Bold("Invite Link:")} https://discord.gg/WC7FRsxFwb";

            await targetChannel.SendMessageAsync(embed: informationChannelEmbed.Build()).ConfigureAwait(false);

            await targetChannel.SendMessageAsync(inviteLink).ConfigureAwait(false);
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [RequireAdminRole]
        [Command("mute")]
        public async Task MuteHelpAsync(CommandContext ctx)
        {
            string toSend =
                "**[SYNTAX]** !mute [USERMENTION] [REASON]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [RequireAdminRole]
        [Command("unmute")]
        public async Task UnmuteHelpAsync(CommandContext ctx)
        {
            string toSend =
                "**[SYNTAX]** !unmute [USERMENTION]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [RequireAdminRole]
        [Command("kick")]
        public async Task KickHelpAsync(CommandContext ctx)
        {
            string toSend =
                "**[SYNTAX]** !kick [USERMENTION] [REASON]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [RequireAdminRole]
        [Command("ban")]
        public async Task BanHelpAsync(CommandContext ctx)
        {
            string toSend = "**[SYNTAX]** !ban [USERMENTION] [REASON]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [RequireAdminRole]
        [Command("unban")]
        public async Task UnbanHelpAsync(CommandContext ctx)
        {
            string toSend = "**[SYNTAX]** !unban [USERID] [REASON (optional)]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [RequireMainGuild, RequireAccessRole]
        [Command("setname")]
        public async Task SetNameAsync(CommandContext ctx)
        {
            string toSend = "**[SYNTAX]** !setname [USERMENTION] [NEWNICKNAME]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [RequireAdminRole]
        [Command("announce")]
        public async Task AnnounceHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} !announce [CHANNEL] [TAG (role / member to mention)] [MESSAGE]");
        }
    }
}
