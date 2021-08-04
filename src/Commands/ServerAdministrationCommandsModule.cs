using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using discordbot.Attributes;

namespace discordbot.Commands
{
    class ServerAdministrationCommandsModule : BaseCommandModule
    {
        [RequireAdminRole]
        [Command("mute")]
        public async Task Mute(CommandContext ctx, DiscordMember member, params string[] muteReason)
        {
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

            var mutedRole = ctx.Guild.Roles.SingleOrDefault(x => x.Value.Name == "Muted").Value.Id;
            await member.GrantRoleAsync(ctx.Guild.GetRole(mutedRole));
            string mutingUser = ctx.Member.DisplayName;

            string message =
                "**[MUTED]** " + member.DisplayName + " has been muted by " + mutingUser + 
                ". Reason: " + string.Join(" ", muteReason);
            await ctx.Channel.SendMessageAsync(message).ConfigureAwait(false);
        }

        [RequireAdminRole]
        [Command("unmute")]
        public async Task Unmute(CommandContext ctx, DiscordMember member)
        {
            // Checks whether the invoker is manually verifying themself.
            if (await ClientUtilities.CheckSelfTargeting(member, ctx))
            {
                return;
            }

            var mutedRole = ctx.Guild.Roles.SingleOrDefault(x => x.Value.Name == "Muted").Value.Id;
            await member.RevokeRoleAsync(ctx.Guild.GetRole(mutedRole));
            string unmutingUser = ctx.Member.DisplayName;

            string message =
                "**[UNMUTED]** " + member.DisplayName + " has been unmuted by " + unmutingUser + ".";
            await ctx.Channel.SendMessageAsync(message).ConfigureAwait(false);
        }

        [RequireAdminRole]
        [Command("kick")]
        public async Task Kick(CommandContext ctx, DiscordMember member, params string[] kickReason)
        {
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

        [RequireMainGuild, RequireAccessRole]
        [Command("setname")]
        public async Task SetName(CommandContext ctx, DiscordMember member, params string[] newNickname)
        {
            if (ctx.User.Id != member.Id)
            {
                bool isAdmin = await ClientUtilities.CheckAdminPermissions(ctx);

                if (!isAdmin)
                {
                    return;
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

            string inviteLink = $"{Formatter.Bold("Invite Link:")} https://discord.gg/WC7FRsxFwb";

            await targetChannel.SendMessageAsync(embed: informationChannelEmbed.Build()).ConfigureAwait(false);

            await targetChannel.SendMessageAsync(inviteLink).ConfigureAwait(false);
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [RequireAdminRole]
        [Command("mute")]
        public async Task MuteHelp(CommandContext ctx)
        {
            string toSend =
                "**[SYNTAX]** !mute [USERMENTION] [REASON (optional)]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [RequireAdminRole]
        [Command("unmute")]
        public async Task UnmuteHelp(CommandContext ctx)
        {
            string toSend =
                "**[SYNTAX]** !unmute [USERMENTION]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [RequireAdminRole]
        [Command("kick")]
        public async Task KickHelp(CommandContext ctx)
        {
            string toSend =
                "**[SYNTAX]** !kick [USERMENTION] [REASON]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [RequireMainGuild, RequireAccessRole]
        [Command("setname")]
        public async Task SetName(CommandContext ctx)
        {
            string toSend = "**[SYNTAX]** !setname [USERMENTION] [NEWNICKNAME]";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }
    }
}
