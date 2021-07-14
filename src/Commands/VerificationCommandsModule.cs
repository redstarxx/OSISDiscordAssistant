using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Threading;
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
            if (!await ClientUtilities.CheckAdminPermissions(ctx.User.Id, ctx))
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
            if (!await ClientUtilities.CheckAdminPermissions(ctx.User.Id, ctx))
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

            //Sends a verification request to #verification as an embed.
            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = "Verification Request",
                Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n" 
                + $"**Nama Panggilan:** {string.Join(" ", displayName)}\n **User ID:** {ctx.User.Id}\n **Verification Status:** WAITING.\n"
                + "Click the checkmark emoji to approve this request or the crossmark emoji to deny. "
                + $"This request expires in two days ({ClientUtilities.GetWesternIndonesianDateTime().AddDays(2)}).",
                Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                },
                Color = DiscordColor.MidnightBlue
            };

            DiscordChannel channel = ctx.Guild.GetChannel(841207483648311336);
            var requestEmbed = await channel.SendMessageAsync(embed: embedBuilder).ConfigureAwait(false);

            string receiptMessage = $"{ctx.Member.Mention}, your verification request has been sent! Expect a response within the next two days!";
            await ctx.Channel.SendMessageAsync(receiptMessage).ConfigureAwait(false);

            var checkmarkEmoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            var crossmarkEmoji = DiscordEmoji.FromName(ctx.Client, ":negative_squared_cross_mark:");

            await requestEmbed.CreateReactionAsync(checkmarkEmoji).ConfigureAwait(false);
            await requestEmbed.CreateReactionAsync(crossmarkEmoji).ConfigureAwait(false);

            // Task needs to be delayed otherwise the interactivity function will accidentally pick up the created emojis.
            Thread.Sleep(TimeSpan.FromSeconds(1));

            DiscordMember member = await ctx.Guild.GetMemberAsync(ctx.User.Id);

            var interactivity = ctx.Client.GetInteractivity();
            var reactionResult = await interactivity.WaitForReactionAsync
                (x => x.Message == requestEmbed && (x.Emoji == checkmarkEmoji || x.Emoji == crossmarkEmoji), 
                TimeSpan.FromDays(2));

            if (!reactionResult.TimedOut)
            {
                if (reactionResult.Result.Emoji == checkmarkEmoji)
                {
                    await member.GrantRoleAsync(ctx.Guild.GetRole(814450965565800498));
                    await member.ModifyAsync(setName => setName.Nickname = string.Join(" ", displayName));
                    await requestEmbed.DeleteAllReactionsAsync();

                    var requestAcceptedEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Verification Request",
                        Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n" 
                        + $"**Nama Panggilan:** {string.Join(" ", displayName)}\n **User ID:** {ctx.User.Id}\n **Verification Status:** ACCEPTED.\n"
                        + "Click the checkmark emoji to approve this request or the crossmark emoji to deny. "
                        + $"This request expires in two days ({ClientUtilities.GetWesternIndonesianDateTime().AddDays(2)}).",
                        Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = "OSIS Discord Assistant"
                        },
                        Color = DiscordColor.MidnightBlue
                    };

                    await requestEmbed.ModifyAsync(embed: requestAcceptedEmbed.Build()).ConfigureAwait(false);

                    string toSend =
                        $"**[USERINFO]** {ctx.Member.Mention}'s verification request has been approved. " +
                        "Access role has been assigned.";
                    await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
                }

                else if (reactionResult.Result.Emoji == crossmarkEmoji)
                {
                    await requestEmbed.DeleteAllReactionsAsync();
                    var requestDeniedEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Verification Request",
                        Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n"
                        + $"**Nama Panggilan:** {string.Join(" ", displayName)}\n **User ID:** {ctx.User.Id}\n **Verification Status:** DENIED.\n"
                        + "Click the checkmark emoji to approve this request or the crossmark emoji to deny. "
                        + $"This request expires in two days ({ClientUtilities.GetWesternIndonesianDateTime().AddDays(2)}).",
                        Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = "OSIS Discord Assistant"
                        },
                        Color = DiscordColor.MidnightBlue
                    };

                    await requestEmbed.ModifyAsync(embed: requestDeniedEmbed.Build()).ConfigureAwait(false);

                    string toSend =
                        $"**[USERINFO]** {ctx.Member.Mention}'s verification request has been denied.";
                    await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
                }
            }

            else
            {
                await requestEmbed.DeleteAllReactionsAsync();
                var timeOutEmbed = new DiscordEmbedBuilder
                {
                    Title = "Verification Request",
                    Description = $"{ctx.User.Username}#{ctx.User.Discriminator} has submitted a verification request.\n"
                + $"**Nama Panggilan:** {string.Join(" ", displayName)}\n **User ID:** {ctx.User.Id}\n **Verification Status:** EXPIRED.\n"
                + "Click the checkmark emoji to approve this request or the crossmark emoji to deny. "
                + $"This request expires in two days ({ClientUtilities.GetWesternIndonesianDateTime().AddDays(2)}).",
                    Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "OSIS Discord Assistant"
                    },
                    Color = DiscordColor.MidnightBlue
                };

                await requestEmbed.ModifyAsync(embed: timeOutEmbed.Build()).ConfigureAwait(false);

                string toSend =
                    "**[USERINFO]** " + string.Join(" ", displayName) + "'s verification request has expired.";
                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
            }
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [Command("overify")]
        public async Task OverrideVerifyHelp(CommandContext ctx)
        {
            // Checks whether the invoker has either of the two roles below.
            await ClientUtilities.CheckAdminPermissions(ctx.User.Id, ctx);

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
