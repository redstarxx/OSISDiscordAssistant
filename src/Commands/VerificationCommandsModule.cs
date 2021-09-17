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
                // Grants the "verified" role to the targeted user. 814450965565800498 as Verified role ID.
                await member.GrantRoleAsync(ctx.Guild.GetRole(814450965565800498));

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
                // Assigns the verified role to the targeted user. "814450965565800498" as Verified role ID.
                await member.GrantRoleAsync(ctx.Guild.GetRole(814450965565800498));
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

        [RequireMainGuild, RequireChannel(832275177160048711)]
        [Command("requestverify")]
        public async Task RequestVerifyAsync(CommandContext ctx, params string[] displayName)
        {
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
                string toSend = $"{Formatter.Bold("[ERROR]")} You have already been verified!";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);

                return;
            }

            using (var db = new CounterContext())
            {
                int counter = db.Counter.SingleOrDefault(x => x.Id == 1).VerifyCounter;

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
                    Title = $"Verification Request #{counter}",
                    Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n"
                        + $"{Formatter.Bold("Nama Panggilan:")} {string.Join(" ", displayName)}\n{Formatter.Bold("User ID:")} {ctx.User.Id}\n{Formatter.Bold("Verification Status:")} WAITING.\n"
                        + $"Click the {Formatter.InlineCode("ACCEPT")} button to approve this request or the {Formatter.InlineCode("DECLINE")} button to deny. "
                        + $"This request expires in two days ({ClientUtilities.GetWesternIndonesianDateTime().AddDays(2)}).",
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

                DiscordChannel channel = ctx.Guild.GetChannel(841207483648311336);
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
                        await member.GrantRoleAsync(ctx.Guild.GetRole(814450965565800498));
                        await member.ModifyAsync(setName => setName.Nickname = requestedName);

                        embedBuilder.Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n"
                            + $"{Formatter.Bold("Nama Panggilan:")} {requestedName}\n{Formatter.Bold("User ID:")} {ctx.User.Id}\n{Formatter.Bold("Verification Status:")} ACCEPTED (handled by {reactionResult.Result.Interaction.User.Mention} at <t:{reactionResult.Result.Interaction.CreationTimestamp.ToUnixTimeSeconds()}:F>).\n"
                            + $"Click the {Formatter.InlineCode("ACCEPT")} button to approve this request or the {Formatter.InlineCode("DECLINE")} button to deny. "
                            + $"This request expires in two days ({ClientUtilities.GetWesternIndonesianDateTime().AddDays(2)}).";
                        embedBuilder.Timestamp = ClientUtilities.GetWesternIndonesianDateTime();

                        await reactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage
                            , new DiscordInteractionResponseBuilder().AddEmbed(embed: embedBuilder));

                        string toSend =
                            $"{Formatter.Bold("[USERINFO]")} {ctx.Member.Mention}'s verification request has been approved. Access role has been assigned.";
                        await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);

                        Counter rowToUpdate = null;
                        rowToUpdate = db.Counter.SingleOrDefault(x => x.Id == 1);

                        if (rowToUpdate != null)
                        {
                            int incrementNumber = counter + 1;
                            rowToUpdate.VerifyCounter = incrementNumber;
                        }

                        db.SaveChanges();

                        DiscordChannel welcomeChannel = ctx.Guild.GetChannel(814450803464732722);

                        await welcomeChannel.SendMessageAsync($"Selamat datang {ctx.Member.Mention}! {DiscordEmoji.FromName(ctx.Client, ":omculikaku:")}").ConfigureAwait(false);
                    }

                    else if (reactionResult.Result.Id == "decline_button")
                    {
                        embedBuilder.Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n"
                            + $"{Formatter.Bold("Nama Panggilan:")} {requestedName}\n{Formatter.Bold("User ID:")} {ctx.User.Id}\n{Formatter.Bold("Verification Status:")} DECLINED (handled by {reactionResult.Result.Interaction.User.Mention} at <t:{reactionResult.Result.Interaction.CreationTimestamp.ToUnixTimeSeconds()}:F>).\n"
                            + $"Click the {Formatter.InlineCode("ACCEPT")} button to approve this request or the {Formatter.InlineCode("DECLINE")} button to deny. "
                            + $"This request expires in two days ({ClientUtilities.GetWesternIndonesianDateTime().AddDays(2)}).";
                        embedBuilder.Timestamp = ClientUtilities.GetWesternIndonesianDateTime();

                        await reactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage
                            , new DiscordInteractionResponseBuilder().AddEmbed(embed: embedBuilder));

                        string toSend = $"{Formatter.Bold("[USERINFO]")} Sorry, {ctx.Member.Mention}! Your verification request has been declined. Contact someone with the {Formatter.InlineCode("Administrator")} role to find out why.";
                        await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);

                        Counter rowToUpdate = null;
                        rowToUpdate = db.Counter.SingleOrDefault(x => x.Id == 1);

                        if (rowToUpdate != null)
                        {
                            int incrementNumber = counter + 1;
                            rowToUpdate.VerifyCounter = incrementNumber;
                        }

                        db.SaveChanges();
                    }
                }

                else
                {
                    embedBuilder.Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n"
                        + $"{Formatter.Bold("Nama Panggilan:")} {requestedName}\n{Formatter.Bold("User ID:")} {ctx.User.Id}\n{Formatter.Bold("Verification Status:")} EXPIRED (at <t:{reactionResult.Result.Interaction.CreationTimestamp.ToUnixTimeSeconds()}:F>).\n"
                        + $"Click the {Formatter.InlineCode("ACCEPT")} button to approve this request or the {Formatter.InlineCode("DECLINE")} button to deny. "
                        + $"This request expires in two days ({ClientUtilities.GetWesternIndonesianDateTime().AddDays(2)}).";
                    embedBuilder.Timestamp = ClientUtilities.GetWesternIndonesianDateTime();

                    await reactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage
                        , new DiscordInteractionResponseBuilder().AddEmbed(embed: embedBuilder));

                    string toSend =
                        $"{Formatter.Bold("[USERINFO]")} Sorry, {ctx.Member.Mention}! Your verification request has expired (nobody responded). Contact someone with the {Formatter.InlineCode("Administrator")} role to find out why.";
                    await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);

                    Counter rowToUpdate = null;
                    rowToUpdate = db.Counter.SingleOrDefault(x => x.Id == 1);

                    if (rowToUpdate != null)
                    {
                        int incrementNumber = counter + 1;
                        rowToUpdate.VerifyCounter = incrementNumber;
                    }

                    db.SaveChanges();
                }
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
