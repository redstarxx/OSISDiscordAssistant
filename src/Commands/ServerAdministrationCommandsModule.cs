using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using OSISDiscordAssistant.Attributes;
using OSISDiscordAssistant.Utilities;
using OSISDiscordAssistant.Services;

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
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You cannot mute yourself.");

                return;
            }

            // Checks whether the command is executed with a reason.
            if (muteReason is null)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You must specify a reason.");

                return;
            }

            ulong mutedRoleId = 0;

            try
            {
                mutedRoleId = ctx.Guild.Roles.SingleOrDefault(x => x.Value.Name == "Muted").Value.Id;
            }

            // If the Muted role does not exist, the bot will try to create one.
            catch
            {
                var creatingRoleMessage = await ctx.Channel.SendMessageAsync($"Setting up {Formatter.InlineCode("Muted")} role...");

                var memberHighestRole = member.Roles.Max(x => x.Position);

                DiscordRole mutedRole = await ctx.Guild.CreateRoleAsync("Muted", Permissions.None, DiscordColor.Red, false, false, "Muted role was not present on this server.");

                await mutedRole.ModifyPositionAsync(memberHighestRole, null);

                var channels = await ctx.Guild.GetChannelsAsync();

                foreach (var channel in channels)
                {
                    await channel.AddOverwriteAsync(mutedRole, Permissions.None, Permissions.SendMessages);
                }

                await ctx.Channel.DeleteMessageAsync(creatingRoleMessage);
                await ctx.Member.SendMessageAsync($"I have created a {Formatter.InlineCode("Muted")} role for your server ({Formatter.Bold(ctx.Guild.Name)})! If you are going to create a new channel, you may need to adjust the role overrides for each channels one by one to maintain the Muted role effect.");

                mutedRoleId = ctx.Guild.Roles.SingleOrDefault(x => x.Value.Name == "Muted").Value.Id;
            }

            await member.GrantRoleAsync(ctx.Guild.GetRole(mutedRoleId));

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[MUTED]")} {member.Mention} has been muted by {ctx.Member.Mention}. Reason: {muteReason}");
        }

        [RequireAdminRole]
        [Command("unmute")]
        public async Task UnmuteAsync(CommandContext ctx, DiscordMember member)
        {
            // Checks whether the invoker is unmuting themself.
            if (member == ctx.Member)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You cannot unmute yourself.");

                return;
            }

            var mutedRole = ctx.Guild.Roles.SingleOrDefault(x => x.Value.Name == "Muted").Value.Id;
            await member.RevokeRoleAsync(ctx.Guild.GetRole(mutedRole));

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[UNMUTED]")} {member.Mention} has been unmuted by {ctx.Member.Mention}.");
        }

        [RequireAdminRole]
        [Command("kick")]
        public async Task KickAsync(CommandContext ctx, DiscordMember member, [RemainingText] string kickReason)
        {
            //Check if the invoker is trying to kick themself.
            if (member == ctx.Member)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Are you trying to kill yourself?");

                return;
            }

            // Checks whether the command is executed with a reason.
            if (kickReason is null)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You must specify a reason.");

                return;
            }

            await member.RemoveAsync(kickReason);

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[KICKED]")} {member.Mention} has been kicked from this server by {ctx.Member.Mention}. Reason: {kickReason}");
        }

        [Command("setname")]
        public async Task SetNameAsync(CommandContext ctx, DiscordMember member, [RemainingText] string newNickname)
        {
            if (ctx.Member != member)
            {
                if (!ClientUtilities.CheckAdminPermissions(ctx))
                {
                    await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} If you are not an administrator, you cannot rename someone else's nickname!");

                    return;
                }
            }

            // Checks whether the command is executed with a reason.
            if (newNickname is null)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You must specify a new name.");

                return;
            }

            string previousNickname = member.DisplayName;
            await member.ModifyAsync(x => x.Nickname = newNickname);

            await ctx.Channel.SendMessageAsync($"{member.Mention} {previousNickname}'s server username has been changed to {newNickname} by {ctx.Member.Mention}.");
        }

        [RequireAdminRole]
        [Command("announce")]
        public async Task AnnounceAsync(CommandContext ctx, DiscordChannel channel, DiscordRole role, [RemainingText] string announceMessage)
        {
            if (announceMessage is null)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} Announcement message cannot be empty!");

                return;
            }

            await channel.SendMessageAsync($"{Formatter.Bold("[ANNOUNCEMENT]")} {role.Mention} {announceMessage}");
        }

        [RequireAdminRole]
        [Command("ban")]
        public async Task BanAsync(CommandContext ctx, DiscordMember member, [RemainingText] string reason)
        {
            if (member == ctx.Member)
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

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[BANNED]")} {member.Mention} has been banned by {ctx.Member.Mention}. Reason: {reason}");
        }

        [RequireAdminRole]
        [Command("unban")]
        public async Task UnbanAsync(CommandContext ctx, DiscordUser member, [RemainingText] string reason)
        {
            if (member == ctx.Member)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} You cannot unban yourself!");

                return;
            }

            DiscordGuild guild = ctx.Member.Guild;

            reason = reason is null ? reason = "N/A" : reason;

            await member.UnbanAsync(guild, reason);

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[UNBANNED]")} {member.Mention} has been unbanned by {ctx.Member.Mention}. Reason: {reason}");
        }

        [RequireAdminRole]
        [Command("prune")]
        public async Task PruneAsync(CommandContext ctx, int messageCount, [RemainingText] string reason)
        {
            messageCount++;

            if (messageCount < 1)
            {
                return;
            }
                
            if (messageCount > 100)
            {
                messageCount = 100;
            }

            reason = reason is null ? reason = "N/A" : reason;

            try
            {
                await ctx.Channel.DeleteMessagesAsync(await ctx.Channel.GetMessagesAsync(messageCount), reason);

                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[PRUNED]")} Pruned {messageCount} messages by {ctx.Member.Mention}. Reason: {reason}.");
            }

            catch
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} An error occured. Are you trying to delete messages older than 14 days?");
            }
        }

        [RequireAdminRole]
        [Command("assignrole")]
        public async Task AssignRoleAsync(CommandContext ctx, DiscordMember member, [RemainingText] string roleName)
        {
            var selectedRole = ctx.Guild.Roles.FirstOrDefault(x => x.Value.Name.ToLowerInvariant().Contains(roleName.ToLowerInvariant())).Value;

            if (selectedRole is null)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} There are no roles in this server that contains the word {Formatter.Bold($"{roleName}")}! Make sure you are typing the {Formatter.Underline("exact")} name of the role you are assigning.");

                return;
            }

            if (member.Roles.Any(x => x == selectedRole))
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} The member has the role {Formatter.Bold($"{selectedRole.Name}")} assigned already!");

                return;
            }

            else
            {
                await member.GrantRoleAsync(selectedRole);

                await ctx.Channel.SendMessageAsync($"Assigned role {Formatter.Bold($"{selectedRole.Name}")} to {member.Mention}.");
            }
        }

        [RequireAdminRole]
        [Command("revokerole")]
        public async Task RevokeRoleAsync(CommandContext ctx, DiscordMember member, [RemainingText] string roleName)
        {
            var selectedRole = ctx.Guild.Roles.FirstOrDefault(x => x.Value.Name.ToLowerInvariant().Contains(roleName.ToLowerInvariant())).Value;

            if (selectedRole is null)
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} There are no roles in this server that contains the word {Formatter.Bold($"{roleName}")}! Make sure you are typing the {Formatter.Underline("exact")} name of the role you are assigning.");

                return;
            }

            if (!member.Roles.Any(x => x == selectedRole))
            {
                await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} The member does not have the role {Formatter.Bold($"{selectedRole.Name}")} assigned!");

                return;
            }

            else
            {
                await member.RevokeRoleAsync(selectedRole);

                await ctx.Channel.SendMessageAsync($"Revoked role {Formatter.Bold($"{selectedRole.Name}")} from {member.Mention}.");
            }
        }

        [RequireAdminRole]
        [Command("lockdown")]
        public async Task LockdownChannelAsync(CommandContext ctx)
        {
            IReadOnlyList<DiscordOverwrite> channelPermissionOverwrites = ctx.Channel.PermissionOverwrites;

            foreach (DiscordOverwrite permissions in channelPermissionOverwrites)
            {
                if (ctx.Guild.Id == SharedData.MainGuildId)
                {
                    if (permissions.Id == SharedData.AccessRoleId)
                    {
                        if (permissions.Denied.HasPermission(Permissions.SendMessages))
                        {
                            await UnlockChannel(ctx, permissions);

                            break;
                        }

                        else
                        {
                            await LockdownChannel(ctx, permissions);

                            break;
                        }
                    }
                }

                else
                {
                    if (permissions.Id == ctx.Guild.EveryoneRole.Id)
                    {
                        if (permissions.Denied.HasPermission(Permissions.SendMessages))
                        {
                            await UnlockChannel(ctx, permissions);

                            break;
                        }

                        else
                        {
                            await LockdownChannel(ctx, permissions);

                            break;
                        }
                    }
                }               
            }
        }

        private async Task LockdownChannel(CommandContext ctx, DiscordOverwrite permissions)
        {
            await permissions.UpdateAsync(Permissions.AccessChannels, Permissions.SendMessages, $"{ctx.User.Username}#{ctx.User.Discriminator} has locked down {ctx.Channel.Name}.");

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[LOCKDOWN]")} This channel has been locked down for the time being. It will be unlocked as soon as the situation has been resolved. {DiscordEmoji.FromName(ctx.Client, ":man_police_officer:")}");
        }

        private async Task UnlockChannel(CommandContext ctx, DiscordOverwrite permissions)
        {
            await permissions.UpdateAsync(Permissions.AccessChannels | Permissions.SendMessages, Permissions.None, $"{ctx.User.Username}#{ctx.User.Discriminator} has unlocked {ctx.Channel.Name}.");

            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[UNLOCKED]")} This channel has been unlocked as the situation has been resolved. Please abide by the rules as they are in place for a reason. {DiscordEmoji.FromName(ctx.Client, ":man_police_officer:")}");
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [RequireAdminRole]
        [Command("mute")]
        public async Task MuteHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis mute [USER MENTION] [REASON]");
        }

        [RequireAdminRole]
        [Command("unmute")]
        public async Task UnmuteHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis unmute [USER MENTION]");
        }

        [RequireAdminRole]
        [Command("kick")]
        public async Task KickHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis kick [USER MENTION] [REASON]");
        }

        [RequireAdminRole]
        [Command("ban")]
        public async Task BanHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis ban [USER MENTION] [REASON]");
        }

        [RequireAdminRole]
        [Command("unban")]
        public async Task UnbanHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis unban [USER ID] [REASON (optional)]");
        }

        [RequireAdminRole]
        [Command("setname")]
        public async Task SetNameAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis setname [USER MENTION] [NEW NICKNAME]");
        }

        [RequireAdminRole]
        [Command("announce")]
        public async Task AnnounceHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis announce [CHANNEL] [TAG (role / member to mention)] [MESSAGE]");
        }

        [RequireAdminRole]
        [Command("prune")]
        public async Task PruneHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis prune [MESSAGE COUNT] [REASON (optional)]");
        }

        [RequireAdminRole]
        [Command("assignrole")]
        public async Task AssignRoleHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis assignrole [USER MENTION] [ROLE NAME]");
        }

        [RequireAdminRole]
        [Command("revokerole")]
        public async Task RevokeRoleHelpAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis revokerole [USER MENTION] [ROLE NAME]");
        }
    }
}
