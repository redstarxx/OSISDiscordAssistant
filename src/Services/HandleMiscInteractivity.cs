using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using OSISDiscordAssistant.Models;
using OSISDiscordAssistant.Utilities;

namespace OSISDiscordAssistant.Services
{
    public class HandleMiscInteractivity : IHandleMiscInteractivity
    {
        private readonly ILogger<HandleMiscInteractivity> _logger;
        private readonly VerificationContext _verificationContext;
        private readonly CounterContext _counterContext;

        public HandleMiscInteractivity(ILogger<HandleMiscInteractivity> logger, VerificationContext verificationContext, CounterContext countercontext, DiscordShardedClient shardedClient)
        {
            _logger = logger;
            _verificationContext = verificationContext;
            _counterContext = countercontext;
        }

        /// <summary>
        /// Handles button interactions related to the main guild verification scheme and the verification process as a whole.
        /// </summary>
        /// <param name="client">The client that receives the interaction.</param>
        /// <param name="e">The event arguments passed from the event handler.</param>
        public async Task<Task> HandleVerificationRequests(DiscordClient client, ComponentInteractionCreateEventArgs e)
        {
            if (e.Id == "verify_button")
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                var interactivity = client.GetInteractivity();

                var member = await e.Guild.GetMemberAsync(e.User.Id);

                bool verificationPending;
                bool isVerified = member.Roles.Any(x => x.Id == SharedData.AccessRoleId);

                verificationPending = _verificationContext.Verifications.Any(x => x.UserId == e.User.Id);

                if (verificationPending)
                {
                    DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder()
                    {
                        Content = "You have already requested for a verification! If it has not been handled yet for some time, feel free to reach out to a member of Inti OSIS for assistance.",
                        IsEphemeral = true
                    };

                    await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);

                    return Task.CompletedTask;
                }

                if (isVerified)
                {
                    DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder()
                    {
                        Content = "You are already verified!",
                        IsEphemeral = true
                    };

                    await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);

                    return Task.CompletedTask;
                }

                else
                {
                    DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder()
                    {
                        Content = "Please check your Direct Messages to proceed with your verification!",
                        IsEphemeral = true
                    };

                    await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);

                    if (SharedData.PendingVerificationData.ContainsKey(e.User.Id))
                    {
                        return Task.CompletedTask;
                    }

                    else
                    {
                        SharedData.PendingVerificationData.TryAdd(e.User.Id, e.User);
                    }
                }

                var askMessage = await member.SendMessageAsync($"Silahkan ketik nama panggilan anda. Contoh: {Formatter.InlineCode("Kerwintiro")}.");

                string requestedName = string.Empty;

                while (true)
                {
                    var nameInteractivity = await client.GetInteractivity().WaitForMessageAsync(x => x.Channel is DiscordDmChannel && x.Author == e.User);

                    if (nameInteractivity.Result.Content is not null)
                    {
                        if (nameInteractivity.Result.Content.Length is > 32)
                        {
                            await member.SendMessageAsync($"Panjang nama panggilan anda tidak boleh melebihi 32 karakter (bisa disingkat, contoh: {Formatter.InlineCode("Kerwintiro")} jadi {Formatter.InlineCode("Kerti")}). Silahkan ulangi lagi.");
                        }

                        else
                        {
                            requestedName = nameInteractivity.Result.Content;
                            SharedData.PendingVerificationData.TryRemove(e.User.Id, out DiscordUser user);

                            break;
                        }
                    }
                }

                DiscordDmChannel discordDm = await member.CreateDmChannelAsync();
                await discordDm.TriggerTypingAsync();

                int verificationCounterNumber = 0;

                verificationCounterNumber = _counterContext.Counter.SingleOrDefault(x => x.Id == 1).VerifyCounter;

                Counter rowToUpdate = null;
                rowToUpdate = _counterContext.Counter.SingleOrDefault(x => x.Id == 1);

                if (rowToUpdate != null)
                {
                    rowToUpdate.VerifyCounter = verificationCounterNumber + 1;
                }

                _counterContext.SaveChanges();

                var messageBuilder = new DiscordMessageBuilder();

                //Sends a verification request to #verification as an embed.
                var embedBuilder = new DiscordEmbedBuilder
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = $"{e.User.Username}#{e.User.Discriminator}",
                        IconUrl = e.User.AvatarUrl
                    },
                    Title = $"Verification Request #{verificationCounterNumber} | PENDING",
                    Description = $"{e.User.Username}#{e.User.Discriminator} has submitted a verification request.\n"
                        + $"{Formatter.Bold("Requested Nickname:")} {requestedName}\n{Formatter.Bold("User ID:")} {e.User.Id}\n{Formatter.Bold("Verification Status:")} PENDING.\n"
                        + $"This request expires at {Formatter.Timestamp(DateTime.Now.AddDays(SharedData.MaxPendingVerificationWaitingDay), TimestampFormat.LongDateTime)}.\nAlternatively, use the {Formatter.InlineCode("!overify")} command to manually verify a new member.",
                    Timestamp = DateTime.Now,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    },
                    Color = DiscordColor.MidnightBlue
                };

                messageBuilder.WithEmbed(embed: embedBuilder);
                messageBuilder.AddComponents(new DiscordButtonComponent[]
                {
                        new DiscordButtonComponent(ButtonStyle.Success, "accept_button", "ACCEPT", false, null),
                        new DiscordButtonComponent(ButtonStyle.Danger, "deny_button", "DECLINE", false, null)
                });

                DiscordChannel channel = e.Guild.GetChannel(SharedData.VerificationRequestsProcessingChannelId);
                var requestEmbed = await channel.SendMessageAsync(builder: messageBuilder);

                _verificationContext.Add(new Verification
                {
                    UserId = e.User.Id,
                    VerificationEmbedId = requestEmbed.Id,
                    RequestedName = requestedName
                });

                await _verificationContext.SaveChangesAsync();

                messageBuilder.Clear();

                embedBuilder.Title = embedBuilder.Title.Replace(" | PENDING", string.Empty);
                embedBuilder.Description = embedBuilder.Description.Replace("Alternatively, use the `!overify` command to manually verify a new member.", string.Empty);
                messageBuilder.WithContent($"Your verification request has been sent successfully! Here's a copy of your verification request details.");
                messageBuilder.WithEmbed(embedBuilder);

                await member.SendMessageAsync(messageBuilder);
            }

            else if (e.Id == "accept_button" || e.Id == "deny_button")
            {
                var userDataRow = _verificationContext.Verifications.FirstOrDefault(x => x.VerificationEmbedId == e.Message.Id);

                if (userDataRow is null)
                {
                    _logger.LogError($"Aborted processing verification request embed ID {e.Message.Id}. Data does not exist in the database.");

                    return Task.CompletedTask;
                }

                var member = await e.Guild.GetMemberAsync(userDataRow.UserId);

                if (e.Id == "accept_button")
                {
                    await member.GrantRoleAsync(e.Guild.GetRole(SharedData.AccessRoleId));

                    await member.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} Your verification request has been {Formatter.Bold("ACCEPTED")} by {e.User.Mention}! You may now access the internal channels of {e.Guild.Name} and receive your divisional roles at <#{SharedData.RolesChannelId}>.");

                    var getEmbed = e.Message;

                    DiscordEmbed updatedEmbed = null;

                    foreach (var embed in getEmbed.Embeds.ToList())
                    {
                        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder(embed)
                        {
                            Title = $"{embed.Title.Replace(" | PENDING", " | ACCEPTED")}",
                            Description = $"{member.Username}#{member.Discriminator} has submitted a verification request.\n"
                            + $"{Formatter.Bold("Requested Nickname:")} {userDataRow.RequestedName}\n{Formatter.Bold("User ID:")} {member.Id}\n{Formatter.Bold("Verification Status:")} ACCEPTED (handled by {e.Interaction.User.Mention} at {Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(e.Interaction.CreationTimestamp.ToUnixTimeSeconds()), TimestampFormat.LongDateTime)})."
                        };

                        updatedEmbed = embedBuilder.Build();

                        break;
                    };

                    var messageBuilder = new DiscordMessageBuilder()
                    {
                        Embed = updatedEmbed
                    };

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(messageBuilder));

                    try
                    {
                        await member.ModifyAsync(x => x.Nickname = userDataRow.RequestedName);
                    }

                    catch { }
                }

                else if (e.Id == "deny_button")
                {
                    await member.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} I'm sorry, your verification request has been {Formatter.Bold("DENIED")} by {e.User.Mention}! You may reach out to the denying person directly or a member of Inti OSIS to find out why.");

                    var getEmbed = await e.Channel.GetMessageAsync(e.Message.Id);

                    DiscordEmbed updatedEmbed = null;

                    foreach (var embed in getEmbed.Embeds.ToList())
                    {
                        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder(embed)
                        {
                            Title = $"{embed.Title.Replace(" | PENDING", " | DENIED")}",
                            Description = $"{member.Username}#{member.Discriminator} has submitted a verification request.\n"
                            + $"{Formatter.Bold("Requested Nickname:")} {userDataRow.RequestedName}\n{Formatter.Bold("User ID:")} {member.Id}\n{Formatter.Bold("Verification Status:")} DENIED (handled by {e.Interaction.User.Mention} at {Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(e.Interaction.CreationTimestamp.ToUnixTimeSeconds()), TimestampFormat.LongDateTime)})."
                        };

                        updatedEmbed = embedBuilder.Build();

                        break;
                    };

                    var messageBuilder = new DiscordMessageBuilder()
                    {
                        Embed = updatedEmbed
                    };

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(messageBuilder));
                }

                // Remove the user data immediately, as it serves no purpose hereafter.
                var rowToDelete = _verificationContext.Verifications.SingleOrDefault(x => x.VerificationEmbedId == e.Message.Id);

                _verificationContext.Remove(rowToDelete);
                await _verificationContext.SaveChangesAsync();

                _logger.LogError($"Removed verification request message ID {e.Message.Id}.", DateTime.Now);
            }

            else if (e.Id == "why_button")
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder()
                {
                    Content = $"Although {e.Guild.Name} is a private server, a verification system is set up in the effort to deter server raids and keep unwanted users out, should the invite link is somehow compromised.\n\n" +
                    $"If you are a new member of OSIS Sekolah Djuwita Batam, get verified now to access our internal channels!",
                    IsEphemeral = true
                };

                await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles the interactions related to the available assignable divisional roles in the main guild.
        /// </summary>
        /// <param name="client">The client that receives the interaction.</param>
        /// <param name="e">The event arguments passed from the event handler.</param>
        public async Task<Task> HandleRolesInteraction(DiscordClient client, ComponentInteractionCreateEventArgs e)
        {
            DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder();

            var member = await e.Guild.GetMemberAsync(e.User.Id);

            if (!member.Roles.Any(x => x.Id == SharedData.AccessRoleId))
            {
                followupMessageBuilder.Content = $"To grant yourself a role, you must go through the verification process first. Check it out at <#{SharedData.VerificationInfoChannelId}>.";
                followupMessageBuilder.IsEphemeral = true;

                await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);

                return Task.CompletedTask;
            }

            if (e.Interaction.Data.ComponentType is ComponentType.Button)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                followupMessageBuilder.Content = "Select either one of the options of which division you are assigned to. You can select only one option at a time.";
                followupMessageBuilder.IsEphemeral = true;

                var rolesDropdownOptions = new List<DiscordSelectComponentOption>();

                foreach (var roles in SharedData.AvailableRoles)
                {
                    rolesDropdownOptions.Add(new DiscordSelectComponentOption(roles.RoleName, roles.RoleId.ToString(), null, member.Roles.Contains(e.Guild.GetRole(roles.RoleId)), new DiscordComponentEmoji(DiscordEmoji.FromName(client, SharedData.AvailableRoles.Find(x => x.RoleId == roles.RoleId).RoleEmoji))));
                }

                var rolesDropdown = new DiscordSelectComponent($"roles_dropdown_{DateTimeOffset.Now.ToUnixTimeSeconds()}", null, rolesDropdownOptions, false, 0, 1);

                followupMessageBuilder.AddComponents(rolesDropdown);

                await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);
            }

            else
            {
                await HandleRolesDropdown(e);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles the dropdown interactions related to the available assignable divisional roles in the main guild.
        /// </summary>
        /// <param name="e">The event arguments passed from the event handler.</param>
        private async Task<Task> HandleRolesDropdown(ComponentInteractionCreateEventArgs e)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            var member = await e.Guild.GetMemberAsync(e.User.Id);
            var userRoleIds = member.Roles.Select(r => r.Id);

            // Duplicate the list as a reference for which role to remove if the member owns either one from the list.
            List<SharedData.AssignableRolesInfo> Roles = new List<SharedData.AssignableRolesInfo>(SharedData.AvailableRoles);

            // Iterate over the selected dropdown option values.
            foreach (var id in e.Values)
            {
                Roles.RemoveAll(x => x.RoleId == Convert.ToUInt64(id));

                await member.GrantRoleAsync(e.Guild.GetRole(Convert.ToUInt64(id)));

                break;
            }

            member = await e.Guild.GetMemberAsync(e.User.Id);

            foreach (var roleIds in Roles.ToList())
            {
                if (userRoleIds.Contains(roleIds.RoleId))
                {
                    await member.RevokeRoleAsync(e.Guild.GetRole(roleIds.RoleId));
                }
            }

            return Task.CompletedTask;
        }
    }
}
