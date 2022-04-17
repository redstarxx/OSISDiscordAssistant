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
    public class MainGuildStaticInteractionHandler : IMainGuildStaticInteractionHandler
    {
        private readonly ILogger<MainGuildStaticInteractionHandler> _logger;
        private readonly VerificationContext _verificationContext;
        private readonly CounterContext _counterContext;

        public MainGuildStaticInteractionHandler(ILogger<MainGuildStaticInteractionHandler> logger, VerificationContext verificationContext, CounterContext countercontext, DiscordShardedClient shardedClient)
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
            try
            {
                if (e.Id == "verify_button")
                {
                    var interactivity = client.GetInteractivity();

                    var member = await e.Guild.GetMemberAsync(e.User.Id);

                    // Check if the requesting user has a pending verification request.
                    if (_verificationContext.Verifications.Any(x => x.UserId == e.User.Id))
                    {
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                        DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder()
                        {
                            Content = "Kamu sudah meminta untuk diverifikasi! Apabila permintaanmu belum diproses setelah waktu yang lama, silahkan DM salah satu anggota Inti OSIS untuk bantuan.",
                            IsEphemeral = true
                        };

                        await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);

                        return Task.CompletedTask;
                    }

                    // Check if the requesting user has already been verified.
                    if (member.Roles.Any(x => x.Id == SharedData.AccessRoleId))
                    {
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                        DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder()
                        {
                            Content = "Kamu sudah terverifikasi!",
                            IsEphemeral = true
                        };

                        await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);

                        return Task.CompletedTask;
                    }

                    string requestedName = string.Empty;

                    string modalId = $"verification_request_{ClientUtilities.GetCurrentUnixTimestamp()}";

                    var modal = new DiscordInteractionResponseBuilder()
                        .WithTitle("Form Permintaan Verifikasi")
                        .WithCustomId(modalId)
                        .AddComponents(new TextInputComponent(label: $"Silahkan ketik nama panggilanmu:", customId: "requested_name", value: e.Interaction.User.Username, min_length: 2, max_length: 32, required: true));
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);

                    var nameResult = await interactivity.WaitForModalAsync(modalId, e.Interaction.User, TimeSpan.FromMinutes(10));

                    if (!nameResult.TimedOut)
                    {
                        await nameResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                        requestedName = string.Join("", nameResult.Result.Values.Where(x => x.Key is "requested_name").Select(x => x.Value));

                        DiscordDmChannel discordDm = await member.CreateDmChannelAsync();

                        int verificationCounterNumber = _counterContext.Counter.SingleOrDefault(x => x.Id == 1).VerifyCounter;

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
                                + $"This request expires at {Formatter.Timestamp(DateTime.Now.AddDays(SharedData.MaxPendingVerificationWaitingDay), TimestampFormat.LongDateTime)}.\nAlternatively, use the {Formatter.InlineCode("osis overify")} command to manually verify a new member.",
                            Timestamp = DateTime.Now,
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = "OSIS Discord Assistant"
                            },
                            Color = DiscordColor.MidnightBlue
                        };

                        messageBuilder.WithEmbed(embed: embedBuilder);
                        messageBuilder.AddComponents(GetVerificationVerdictButtons());

                        DiscordChannel channel = e.Guild.GetChannel(SharedData.VerificationRequestsProcessingChannelId);
                        var requestEmbed = await channel.SendMessageAsync(builder: messageBuilder);

                        messageBuilder.Clear();

                        embedBuilder.Title = embedBuilder.Title.Replace(" | PENDING", string.Empty);
                        embedBuilder.Description = embedBuilder.Description.Replace("Alternatively, use the `osis overify` command to manually verify a new member.", string.Empty);
                        messageBuilder.WithContent($"Permintaan verifikasimu telah diterima! Ini adalah duplikat dari detail permintaan verifikasimu.");
                        messageBuilder.WithEmbed(embedBuilder);

                        await member.SendMessageAsync(messageBuilder);

                        await nameResult.Result.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        {
                            Content = "Permintaan verifikasimu telah diterima! Silahkan lihat Direct Messages anda untuk melihat detail permintaan verifikasimu.",
                            IsEphemeral = true
                        });

                        var counter = _counterContext.Counter.SingleOrDefault(x => x.Id == 1);

                        if (counter != null)
                        {
                            counter.VerifyCounter = verificationCounterNumber + 1;

                            _counterContext.SaveChanges();
                        }

                        _verificationContext.Add(new Verification
                        {
                            UserId = e.User.Id,
                            VerificationEmbedId = requestEmbed.Id,
                            RequestedName = requestedName
                        });

                        await _verificationContext.SaveChangesAsync();
                    }

                    else
                    {
                        await nameResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                        await nameResult.Result.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        {
                            Content = "Kamu tidak memasukkan nama panggilanmu dalam 10 menit. Silahkan coba lagi.",
                            IsEphemeral = true
                        });
                    }
                }

                else if (e.Id == "accept_button" || e.Id == "deny_button")
                {
                    var userDataRow = _verificationContext.Verifications.FirstOrDefault(x => x.VerificationEmbedId == e.Message.Id);

                    if (userDataRow is null)
                    {
                        _logger.LogError("Aborted processing verification request embed ID {RequestEmbedMessageId}. Data does not exist in the database.", e.Message.Id);

                        return Task.CompletedTask;
                    }

                    var member = await e.Guild.GetMemberAsync(userDataRow.UserId);

                    var buttonOptions = GetVerificationVerdictButtons();

                    foreach (var button in buttonOptions)
                    {
                        button.Disable();
                    }

                    if (e.Id == "accept_button")
                    {
                        await member.GrantRoleAsync(e.Guild.GetRole(SharedData.AccessRoleId));

                        await member.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} Permintaan verifikasimu telah {Formatter.Bold("DITERIMA")} oleh {e.User.Mention}! Sekarang kamu bisa mengakses channel internal server {e.Guild.Name} dan mendapatkan role seksimu di <#{SharedData.RolesChannelId}>.");

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
                            .WithEmbed(updatedEmbed)
                            .AddComponents(buttonOptions);

                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(messageBuilder));

                        try
                        {
                            await member.ModifyAsync(x => x.Nickname = userDataRow.RequestedName);
                        }

                        catch { }
                    }

                    else if (e.Id == "deny_button")
                    {
                        await member.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} Mohon maaf, permintaan verifikasimu telah {Formatter.Bold("DITOLAK")} oleh {e.User.Mention}! Kamu boleh menghubungi penolak atau anggota Inti OSIS lewat DM untuk mendapatkan alasan penolakan.");

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
                            .WithEmbed(updatedEmbed)
                            .AddComponents(buttonOptions);

                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(messageBuilder));
                    }

                    // Remove the user data immediately, as it serves no purpose hereafter.
                    var rowToDelete = _verificationContext.Verifications.SingleOrDefault(x => x.VerificationEmbedId == e.Message.Id);

                    _verificationContext.Remove(rowToDelete);
                    await _verificationContext.SaveChangesAsync();

                    _logger.LogInformation("Removed verification request message ID {RequestEmbedMessageId}.", e.Message.Id);
                }

                else if (e.Id == "why_button")
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                    DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder()
                    {
                        Content = $"Meskipun {e.Guild.Name} adalah server privat, sistem verifikasi disiapkan dalam upaya untuk mencegah server raid dan mencegah pengguna yang tidak diinginkan, jika invite link bocor.\n\n" +
                         $"Jika Anda adalah anggota baru OSIS Sekolah Djuwita Batam, verifikasikan dirimu sekarang untuk mengakses channel internal server ini!\n\n" +
                         $"{DiscordEmoji.FromName(client, ":green_circle:")} Keuntungan pengguna terverifikasi:\n- Mendapatkan pengingat hari -H event dan pengumpulan proposal,\n- Bisa ikut rapat atau nongkrong online sambil dengerin lagu,\n- Sistem komunikasi antar anggota yang terintegrasi (tidak seperti di platform media sosial lainnya dimana kamu harus buat grup ini dan itu secara terpisah sehingga berantakan.)",
                        IsEphemeral = true
                    };

                    await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);
                }

                return Task.CompletedTask;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while handling ComponentInteractionCreated event (component ID: {ComponentId}).", e.Id);

                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Handles the interactions related to the available assignable divisional roles in the main guild.
        /// </summary>
        /// <param name="client">The client that receives the interaction.</param>
        /// <param name="e">The event arguments passed from the event handler.</param>
        public async Task<Task> HandleRolesInteraction(DiscordClient client, ComponentInteractionCreateEventArgs e)
        {
            try
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                DiscordFollowupMessageBuilder followupMessageBuilder = new DiscordFollowupMessageBuilder();

                var member = await e.Guild.GetMemberAsync(e.User.Id);

                if (!member.Roles.Any(x => x.Id == SharedData.AccessRoleId))
                {
                    followupMessageBuilder.Content = $"Untuk bisa mendapatkan role seksi, kamu harus melewati proses verifikasi terlebih dulu di <#{SharedData.VerificationInfoChannelId}>.";
                    followupMessageBuilder.IsEphemeral = true;

                    await e.Interaction.CreateFollowupMessageAsync(followupMessageBuilder);

                    return Task.CompletedTask;
                }

                if (e.Interaction.Data.ComponentType is ComponentType.Button)
                {
                    followupMessageBuilder.Content = "Pilih seksi dimana kamu ditempatkan. Kamu hanya bisa memilih salah satu saja.";
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

            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while handling ComponentInteractionCreated event (component ID: {ComponentId}).", e.Id);

                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Handles the dropdown interactions related to the available assignable divisional roles in the main guild.
        /// </summary>
        /// <param name="e">The event arguments passed from the event handler.</param>
        private async Task<Task> HandleRolesDropdown(ComponentInteractionCreateEventArgs e)
        {
            try
            {
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

            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while handling ComponentInteractionCreated event (component ID: {ComponentId}).", e.Id);

                return Task.CompletedTask;
            }            
        }

        /// <summary>
        /// Retrieves a pair of buttons which will be attached to the verification request embed.
        /// </summary>
        /// <returns>A <see cref="List{T}" /> of <see cref="DiscordButtonComponent" /> objects, an ACCEPT and DENY button.</returns>
        public List<DiscordButtonComponent> GetVerificationVerdictButtons()
        {
            return new List<DiscordButtonComponent>
            {
                new DiscordButtonComponent(ButtonStyle.Success, "accept_button", "ACCEPT", false, null),
                new DiscordButtonComponent(ButtonStyle.Danger, "deny_button", "DECLINE", false, null)
            };
        }
    }
}
