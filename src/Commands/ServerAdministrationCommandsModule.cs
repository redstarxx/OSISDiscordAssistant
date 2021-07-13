using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace discordbot.Commands
{
    class ServerAdministrationCommandsModule : BaseCommandModule
    {
        [Command("mute")]
        public async Task Mute(CommandContext ctx, DiscordMember member, params string[] muteReason)
        {
            // Checks whether the invoker has either of the two roles below.
            if (!await ClientUtilities.CheckAdminPermissions(ctx.User.Id, ctx))
            {
                return;
            }

            // Checks whether the invoker is manually verifying themself.
            if (await ClientUtilities.CheckSelfTargeting(member, ctx))
            {
                return;
            }

            // Checks whether the command is executed with a reason.
            if (string.Join(" ", muteReason).Length == 0)
            {
                string toSend = "**[ERROR]** You must specify a reason.";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
                return;
            }

            var mutedRole = ctx.Guild.GetRole(832211383281123338);
            await member.GrantRoleAsync(mutedRole);
            string mutingUser = ctx.Member.DisplayName;

            string message =
                "**[MUTED]** " + member.DisplayName + " has been muted by " + mutingUser + 
                ". Reason: " + string.Join(" ", muteReason);
            await ctx.Channel.SendMessageAsync(message).ConfigureAwait(false);
        }

        [Command("unmute")]
        public async Task Unmute(CommandContext ctx, DiscordMember member)
        {
            // Checks whether the invoker has either of the two roles below.
            if (!await ClientUtilities.CheckAdminPermissions(ctx.User.Id, ctx))
            {
                return;
            }

            // Checks whether the invoker is manually verifying themself.
            if (await ClientUtilities.CheckSelfTargeting(member, ctx))
            {
                return;
            }

            var mutedRole = ctx.Guild.GetRole(832211383281123338);
            await member.RevokeRoleAsync(mutedRole);
            string unmutingUser = ctx.Member.DisplayName;

            string message =
                "**[UNMUTED]** " + member.DisplayName + " has been unmuted by " + unmutingUser + ".";
            await ctx.Channel.SendMessageAsync(message).ConfigureAwait(false);
        }

        [Command("kick")]
        public async Task Kick(CommandContext ctx, DiscordMember member, params string[] kickReason)
        {
            // Checks whether the invoker has either of the two roles below.
            if (!await ClientUtilities.CheckAdminPermissions(ctx.User.Id, ctx))
            {
                return;
            }

            // Checks whether the invoker is manually verifying themself.
            if (await ClientUtilities.CheckSelfTargeting(member, ctx))
            {
                return;
            }

            // Checks whether the command is executed with a reason.
            if (string.Join(" ", kickReason).Length == 0)
            {
                string toSend = "**[ERROR]** You must specify a reason.";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
                return;
            }

            try
            {
                await member.RemoveAsync();

                string toSend = 
                    "**[KICKED]** " + member.Username + "#" + member.Discriminator 
                    + " (" + member.DisplayName + ") has been kicked from the guild. Reason: " + string.Join(" ", kickReason);
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
            }

            catch
            {
                string toSend = "**[ERROR]** An error occured. Have you tried to use the command correctly?";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
            }
        }

        [Command("setname")]
        public async Task SetName(CommandContext ctx, DiscordMember member, params string[] newNickname)
        {
            // Checks whether the invoker possesses the OSIS role.
            var roleList = string.Join(", ", ctx.Member.Roles);
            if (!roleList.Contains("OSIS"))
            {
                string errorReason = "**[ERROR]** This command is restricted to verified members only.";
                await ctx.Channel.SendMessageAsync(errorReason).ConfigureAwait(false);
                return;
            }

            if (ctx.User.Id != member.Id)
            {
                if (!roleList.Contains("Service Administrator"))
                {
                    if (!roleList.Contains("Inti OSIS"))
                    {
                        string errorReason = 
                            "**[ERROR]** You must possess an administrator level role to change other user's display name.";
                        await ctx.Channel.SendMessageAsync(errorReason).ConfigureAwait(false);
                        return;
                    }
                }
            }

            // Checks whether the command is executed with a reason.
            if (string.Join(" ", newNickname).Length == 0)
            {
                string toSend = "**[ERROR]** You must specify a new nickname.";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
                return;
            }

            try
            {
                string previousNickname = member.DisplayName;
                await member.ModifyAsync(x => x.Nickname = string.Join(" ", newNickname));

                string toSend = 
                    member.Mention + " " + previousNickname + "'s server username has been changed to " + 
                    string.Join(" ", newNickname) + ".";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
            }

            catch
            {
                string toSend = "**[ERROR]** An error occured. Have you tried to use the command correctly?";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
            }
        }

        [Command("sendinfoembed")]
        public async Task InfoEmbedAsync(CommandContext ctx, ulong targetChannelId)
        {
            DiscordChannel targetChannel = await Bot.Client.GetChannelAsync(targetChannelId);

            var informationChannelEmbed = new DiscordEmbedBuilder
            {
                Title = "OSIS Sekolah Djuwita Batam - Discord Server",
                Description = $"Selamat datang di server Discord {Formatter.Bold("OSIS Sekolah Djuwita Batam!")} Akses ke server ini hanya diperbolehkan untuk anggota OSIS aktif.\n\n" +
                $"Untuk meminta verifikasi akses, ketik {Formatter.InlineCode("!requestverify")} di <#832275177160048711>. Inti OSIS akan memproses permintaan verifikasimu secepatnya. " +
                $"Sebagai alternatif, anggota Inti OSIS dapat langsung memverifikasi aksesmu dengan {Formatter.InlineCode("!overify")}.",
                Color = DiscordColor.MidnightBlue
            };

            await targetChannel.SendMessageAsync(embed: informationChannelEmbed.Build()).ConfigureAwait(false);
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [Command("mute")]
        public async Task MuteHelp(CommandContext ctx)
        {
            // Checks whether the invoker has either of the two roles below.
            if (!await ClientUtilities.CheckAdminPermissions(ctx.User.Id, ctx))
            {
                return;
            }

            string toSend =
                "**[SYNTAX]** !mute [USERMENTION] [REASON (optional)]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [Command("unmute")]
        public async Task UnmuteHelp(CommandContext ctx)
        {
            // Checks whether the invoker has either of the two roles below.
            if (!await ClientUtilities.CheckAdminPermissions(ctx.User.Id, ctx))
            {
                return;
            }

            string toSend =
                "**[SYNTAX]** !unmute [USERMENTION]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [Command("kick")]
        public async Task KickHelp(CommandContext ctx)
        {
            // Checks whether the invoker has either of the two roles below.
            if (!await ClientUtilities.CheckAdminPermissions(ctx.User.Id, ctx))
            {
                return;
            }

            string toSend =
                "**[SYNTAX]** !kick [USERMENTION] [REASON]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [Command("setname")]
        public async Task SetName(CommandContext ctx)
        {
            // Checks whether the invoker possesses the OSIS role.
            var roleList = string.Join(", ", ctx.Member.Roles);
            if (!roleList.Contains("OSIS"))
            {
                string errorReason = "**[ERROR]** This command is restricted to verified members only.";
                await ctx.Channel.SendMessageAsync(errorReason).ConfigureAwait(false);
                return;
            }

            string toSend = "**[SYNTAX]** !setname [USERMENTION] [NEWNICKNAME]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }
    }
}
