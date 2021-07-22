using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Threading;
using System.Linq;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;

namespace discordbot.Commands
{
    class VerificationCommandsModule : BaseCommandModule
    {    
        [Command("overify")]
        public async Task OverrideVerifySingle(CommandContext ctx, DiscordMember member)
        {
            // Checks whether the invoker has either of the two roles below.
            if (!await ClientUtilities.CheckAdminPermissions(ctx))
            {
                return;
            }

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

        [Command("overify")]
        public async Task OverrideVerifyDouble(CommandContext ctx, DiscordMember member, params string[] displayName)
        {
            // Checks whether the invoker has either of the two roles below.
            if (!await ClientUtilities.CheckAdminPermissions(ctx))
            {
                return;
            }

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

        [Command("requestverify")]
        public async Task RequestVerifyMain(CommandContext ctx, params string[] displayName)
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
                    Title = $"Verification Request #{counter}",
                    Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n"
                        + $"{Formatter.Bold("Nama Panggilan:")} {string.Join(" ", displayName)}\n {Formatter.Bold("User ID:")} {ctx.User.Id}\n {Formatter.Bold("Verification Status:")} WAITING.\n"
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
                            + $"{Formatter.Bold("Nama Panggilan:")} {requestedName}\n {Formatter.Bold("User ID:")} {ctx.User.Id}\n {Formatter.Bold("Verification Status:")} ACCEPTED.\n"
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
                    }

                    else if (reactionResult.Result.Id == "decline_button")
                    {
                        embedBuilder.Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n"
                            + $"{Formatter.Bold("Nama Panggilan:")} {requestedName}\n {Formatter.Bold("User ID:")} {ctx.User.Id}\n {Formatter.Bold("Verification Status:")} DECLINED.\n"
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
                        + $"{Formatter.Bold("Nama Panggilan:")} {requestedName}\n {Formatter.Bold("User ID:")} {ctx.User.Id}\n {Formatter.Bold("Verification Status:")} EXPIRED.\n"
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

        [Command("overify")]
        public async Task OverrideVerifyHelp(CommandContext ctx)
        {
            // Checks whether the invoker has either of the two roles below.
            await ClientUtilities.CheckAdminPermissions(ctx);

            string toSend =
                "**[SYNTAX]** !overify [USERMENTION] [DISPLAYNAME (optional)]\n"
                + "Note: Fill in the parameters in sync with above otherwise the bot cannot process your request.";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }

        [Command("requestverify")]
        public async Task RequestVerifyHelp(CommandContext ctx)
        {
            string toSend =
                "**[SYNTAX]** !requestverify [NAMA PANGGILAN]\n"
                + "Note: Fill in the parameters in sync with above otherwise the bot cannot process your request.";
            await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
        }
    }
}
