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
                // Grants the "verified" role to the targeted user.
                await member.GrantRoleAsync(ctx.Guild.GetRole(SharedData.AccessRoleId));

                string toSend =
                    "**[USERINFO]** Informasi akun " + member.DisplayName + " telah diupdate.";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
            }

            catch
            {
                string toSend =
                    "**[ERROR]** An error occured. Have you tried to use the command correctly?";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
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
                // Assigns the verified role to the targeted user.
                await member.GrantRoleAsync(ctx.Guild.GetRole(SharedData.AccessRoleId));
                await member.ModifyAsync(setName => setName.Nickname = string.Join(" ", displayName));

                string toSend =
                    "**[USERINFO]** Informasi akun " + string.Join(" ", displayName) + " telah diupdate.";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
            }

            catch
            {
                string toSend =
                    "**[ERROR]** An error occured. Have you tried to use the command correctly?";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
            }
        }

        [RequireMainGuild]
        [Command("requestverify")]
        public async Task RequestVerifyAsync(CommandContext ctx, params string[] displayName)
        {
            if (ctx.Channel.Id != SharedData.VerificationRequestsCommandChannelId)
            {
                DiscordChannel requiredChannel = await ctx.Client.GetChannelAsync(SharedData.VerificationRequestsCommandChannelId);

                await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} This command is only usable in {requiredChannel.Mention}!");

                return;
            }

            // Checks whether the command is executed with a reason.
            if (string.Join(" ", displayName).Length == 0)
            {
                string toSend =
                "**[SYNTAX]** !requestverify [NAMA PANGGILAN]\n"
                + "Note: Fill in the parameters in sync with above otherwise the bot cannot process your request.";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
                return;
            }

            bool hasAccessRole = ClientUtilities.CheckAccessRole(ctx);

            if (hasAccessRole)
            {
                string toSend = $"{Formatter.Bold("[ERROR]")} You are already verified!";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);

                return;
            }

            int verificationCounterNumber = 0;

            using (var db = new CounterContext())
            {
                verificationCounterNumber = db.Counter.SingleOrDefault(x => x.Id == 1).VerifyCounter;

                Counter rowToUpdate = null;
                rowToUpdate = db.Counter.SingleOrDefault(x => x.Id == 1);

                if (rowToUpdate != null)
                {
                    rowToUpdate.VerifyCounter = verificationCounterNumber + 1;
                }

                db.SaveChanges();              
            }

            string requestedName = string.Join(" ", displayName);

            var messageBuilder = new DiscordMessageBuilder();

            //Sends a verification request to #verification as an embed.
            var embedBuilder = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = $"{ctx.Member.DisplayName}#{ctx.Member.Discriminator}",
                    IconUrl = ctx.Member.AvatarUrl
                },
                Title = $"Verification Request #{verificationCounterNumber}",
                Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n"
                    + $"{Formatter.Bold("Nama Panggilan:")} {string.Join(" ", displayName)}\n{Formatter.Bold("User ID:")} {ctx.User.Id}\n{Formatter.Bold("Verification Status:")} WAITING.\n"
                    + $"Click the {Formatter.InlineCode("ACCEPT")} button to approve this request or the {Formatter.InlineCode("DECLINE")} button to deny. "
                    + $"This request expires in two days ({Formatter.Timestamp(ClientUtilities.GetWesternIndonesianDateTime().AddDays(2), TimestampFormat.LongDateTime)}).",
                Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
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
                    new DiscordButtonComponent(ButtonStyle.Danger, "decline_button", "DECLINE", false, null)
            });

            DiscordChannel channel = ctx.Guild.GetChannel(SharedData.VerificationRequestsProcessingChannelId);
            var requestEmbed = await channel.SendMessageAsync(builder: messageBuilder).ConfigureAwait(false);

            string receiptMessage = $"{ctx.Member.Mention}, your verification request has been sent! Expect a response within the next two days!";
            await ctx.Channel.SendMessageAsync(receiptMessage).ConfigureAwait(false);

            DiscordMember member = await ctx.Guild.GetMemberAsync(ctx.User.Id);

            var interactivity = ctx.Client.GetInteractivity();

            var reactionResult = await interactivity.WaitForButtonAsync(requestEmbed, TimeSpan.FromDays(2));

            if (!reactionResult.TimedOut)
            {
                if (reactionResult.Result.Id == "accept_button")
                {
                    await member.GrantRoleAsync(ctx.Guild.GetRole(SharedData.AccessRoleId));
                    await member.ModifyAsync(setName => setName.Nickname = requestedName);

                    embedBuilder.Title = $"Verification Request #{verificationCounterNumber} | ACCEPTED";
                    embedBuilder.Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n"
                        + $"{Formatter.Bold("Nama Panggilan:")} {requestedName}\n{Formatter.Bold("User ID:")} {ctx.User.Id}\n{Formatter.Bold("Verification Status:")} ACCEPTED (handled by {reactionResult.Result.Interaction.User.Mention} at <t:{reactionResult.Result.Interaction.CreationTimestamp.ToUnixTimeSeconds()}:F>).";
                    embedBuilder.Timestamp = ClientUtilities.GetWesternIndonesianDateTime();

                    await reactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage
                        , new DiscordInteractionResponseBuilder().AddEmbed(embed: embedBuilder));

                    string toSend =
                        $"{Formatter.Bold("[USERINFO]")} {ctx.Member.Mention}'s verification request has been approved. Access role has been assigned.";
                    await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
                }

                else if (reactionResult.Result.Id == "decline_button")
                {
                    embedBuilder.Title = $"Verification Request #{verificationCounterNumber} | DENIED";
                    embedBuilder.Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n"
                        + $"{Formatter.Bold("Nama Panggilan:")} {requestedName}\n{Formatter.Bold("User ID:")} {ctx.User.Id}\n{Formatter.Bold("Verification Status:")} DECLINED (handled by {reactionResult.Result.Interaction.User.Mention} at <t:{reactionResult.Result.Interaction.CreationTimestamp.ToUnixTimeSeconds()}:F>).";
                    embedBuilder.Timestamp = ClientUtilities.GetWesternIndonesianDateTime();

                    await reactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage
                        , new DiscordInteractionResponseBuilder().AddEmbed(embed: embedBuilder));

                    string toSend = $"{Formatter.Bold("[USERINFO]")} Sorry, {ctx.Member.Mention}! Your verification request has been declined. Contact someone with the {Formatter.InlineCode("Administrator")} role to find out why.";
                    await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
                }
            }

            else
            {
                embedBuilder.Title = $"Verification Request #{verificationCounterNumber} | EXPIRED";
                embedBuilder.Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n"
                    + $"{Formatter.Bold("Nama Panggilan:")} {requestedName}\n{Formatter.Bold("User ID:")} {ctx.User.Id}\n{Formatter.Bold("Verification Status:")} EXPIRED (nobody handled this request within 48 hours) at <t:{reactionResult.Result.Interaction.CreationTimestamp.ToUnixTimeSeconds()}:F>).";
                embedBuilder.Timestamp = ClientUtilities.GetWesternIndonesianDateTime();

                await reactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage
                    , new DiscordInteractionResponseBuilder().AddEmbed(embed: embedBuilder));

                string toSend =
                    $"{Formatter.Bold("[USERINFO]")} Sorry, {ctx.Member.Mention}! Your verification request has expired (nobody responded). Contact someone with the {Formatter.InlineCode("Administrator")} role to find out why.";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
            }
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [RequireMainGuild, RequireAdminRole]
        [Command("overify")]
        public async Task OverifyHelpAsync(CommandContext ctx)
        {
            string toSend =
                "**[SYNTAX]** !overify [USERMENTION] [DISPLAYNAME (optional)]\n"
                + "Note: Fill in the parameters in sync with above otherwise the bot cannot process your request.";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [RequireMainGuild]
        [Command("requestverify")]
        public async Task RequestVerifyHelpAsync(CommandContext ctx)
        {
            string toSend =
                "**[SYNTAX]** !requestverify [NAMA PANGGILAN]\n"
                + "Note: Fill in the parameters in sync with above otherwise the bot cannot process your request.";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }
    }
}
