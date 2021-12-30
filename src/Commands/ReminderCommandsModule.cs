using System;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;
using DSharpPlus;
using OSISDiscordAssistant.Utilities;

namespace OSISDiscordAssistant.Commands
{
    class ReminderCommandsModule : BaseCommandModule
    {       
        [Command("remind")]
        public async Task RemindWithChannelAsync(CommandContext ctx, string remindTarget, string timeSpan, DiscordChannel toChannel, params string[] toRemind)
        {
            await CreateReminderAsync(ctx, remindTarget, timeSpan, toChannel, toRemind);
        }

        [Command("remind")]
        public async Task RemindWithoutChannelAsync(CommandContext ctx, string remindTarget, string timeSpan, params string[] toRemind)
        {
            await CreateReminderAsync(ctx, remindTarget, timeSpan, null, toRemind);
        }

        /// <summary>
        /// Creates a reminder which is based from creating a delayed task that sends a message after delaying the task for the specified amount of time.
        /// </summary>
        /// <returns>A reminder task that runs in the background.</returns>
        public async Task CreateReminderAsync(CommandContext ctx, string remindTarget, string timeSpan, DiscordChannel remindChannel = null, params string[] toRemind)
        {
            // Checks whether the message to remind is empty.
            string remindMessage = string.Join(" ", toRemind);

            if (remindMessage.Length == 0)
            {
                string toSend = $"{Formatter.Bold("[ERROR]")} You cannot remind someone with an empty message. Type {Formatter.InlineCode("!remind")} to get help. Alternatively, click the emoji below to get help.";
                var errorMessage = await ctx.RespondAsync(toSend);

                await SendHelpEmoji(ctx, errorMessage);

                return;
            }

            // Determines whether the user intends to remind themselves or @everyone.
            string displayTarget = string.Empty;

            switch (remindTarget)
            {
                case "me":
                    remindTarget = ctx.Member.Mention;
                    displayTarget = "you";
                    break;
                case "@everyone":
                    remindTarget = "@everyone";
                    displayTarget = "everyone";
                    break;
                case "everyone":
                    remindTarget = "@everyone";
                    displayTarget = "everyone";
                    break;
                default:
                    string toCheck = remindTarget.Remove(2);
                    if (remindTarget.StartsWith("<") && toCheck == "<@")
                    {
                        displayTarget = remindTarget;

                        if (remindTarget == ctx.User.Mention)
                        {
                            displayTarget = "you";
                        }
                    }

                    else
                    {
                        string toSend = $"{Formatter.Bold("[ERROR]")} Looks like an invalid reminder target! Type {Formatter.InlineCode("!remind")} to get help. Alternatively, click the emoji below to get help.";
                        var errorMessage = await ctx.RespondAsync(toSend);

                        await SendHelpEmoji(ctx, errorMessage);

                        return;
                    }

                    break;
            }

            DiscordChannel targetChannel = remindChannel ?? ctx.Channel;

            DateTime currentTime = DateTime.Now;

            if (timeSpan.Contains("/"))
            {
                DateTime remindTime;

                try
                {
                    remindTime = DateTime.ParseExact(string.Join(" ", timeSpan), "dd/MM/yyyy", null, DateTimeStyles.None);
                }

                catch
                {
                    try
                    {
                        remindTime = DateTime.ParseExact(string.Join(" ", timeSpan), "MM/dd/yyyy", null, DateTimeStyles.None);
                    }

                    catch
                    {
                        try
                        {
                            remindTime = DateTime.ParseExact(string.Join(" ", timeSpan), "dd/MMM/yyyy", null, DateTimeStyles.None);
                        }

                        catch
                        {
                            string errorMessage =
                                "**[ERROR]** An error occured while parsing your date. Acceptable date formats are " +
                                "`DD/MM/YYYY`, `MM/DD/YYYY` or `DD/MMM/YYYY`. \nExample: 25/06/2019, 06/25/2019, 25/JUN/2019.";
                            await ctx.RespondAsync(errorMessage);

                            return;
                        }
                    }
                }

                TimeSpan remainingTime = remindTime - currentTime;

                // Checks whether the provided time span is not less than 30 seconds.
                if (remainingTime.TotalSeconds < 30)
                {
                    string errorMessage = "**[ERROR]** Minimum allowed time span is 30 seconds.";
                    await ctx.RespondAsync(errorMessage);

                    return;
                }

                else
                {
                    if (remainingTime.Days > 365)
                    {
                        string errorMessage = "**[ERROR]** Maximum allowed time span is one year.";
                        await ctx.RespondAsync(errorMessage);

                        return;
                    }

                    ClientUtilities.CreateReminderTask(remainingTime, targetChannel, remindMessage, ctx, remindTarget);

                    await ctx.Channel.SendMessageAsync(ClientUtilities.CreateReminderReceiptMessage(remainingTime, remindMessage, displayTarget));                 
                }
            }

            else if (timeSpan.Contains(":"))
            {
                var remindTime = DateTime.ParseExact(timeSpan, "H:mm", null, DateTimeStyles.None);

                if (currentTime > remindTime)
                {
                    remindTime = remindTime.AddDays(1);
                }

                TimeSpan remainingTime = remindTime - currentTime;

                ClientUtilities.CreateReminderTask(remainingTime, targetChannel, remindMessage, ctx, remindTarget);

                await ctx.Channel.SendMessageAsync(ClientUtilities.CreateReminderReceiptMessage(remainingTime, remindMessage, displayTarget));
            }

            else
            {
                try
                {
                    TimeSpan remainingTime = ClientUtilities.ParseToSeconds(timeSpan);

                    DateTime remindTime = currentTime + remainingTime;

                    // Checks whether the provided time span is not less than 30 seconds.
                    if (remainingTime.TotalSeconds < 30)
                    {
                        string errorMessage = "**[ERROR]** Minimum allowed time span is 30 seconds.";
                        await ctx.RespondAsync(errorMessage);

                        return;
                    }

                    else
                    {
                        if (remainingTime.Days > 365)
                        {
                            string errorMessage = "**[ERROR]** Maximum allowed time span is one year.";
                            await ctx.RespondAsync(errorMessage);

                            return;
                        }

                        ClientUtilities.CreateReminderTask(remainingTime, targetChannel, remindMessage, ctx, remindTarget);

                        await ctx.Channel.SendMessageAsync(ClientUtilities.CreateReminderReceiptMessage(remainingTime, remindMessage, displayTarget));                        
                    }
                }

                catch
                {
                    var errorMessage = await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} An error occured. Have you tried to use the command correctly? Type {Formatter.InlineCode("!remind")} to get help. Alternatively, click the emoji below to get help.");

                    await SendHelpEmoji(ctx, errorMessage);
                }
            }
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [Command("remind")]
        public async Task RemindHelpAsync(CommandContext ctx)
        {
            await SendHelpMessage(ctx);
        }

        internal async Task SendHelpMessage(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} !remind [ROLE / MEMBER] [TANGGAL / WAKTU UNTUK DIINGATKAN (example: 25/06/2021 or 6j30m or 12:30 or 30m)] [CHANNEL (optional)] [MESSAGE]");
        }

        internal async Task SendHelpEmoji(CommandContext ctx, DiscordMessage errorMessage)
        {
            var helpEmoji = DiscordEmoji.FromName(ctx.Client, ":sos:");

            await errorMessage.CreateReactionAsync(helpEmoji);

            var interactivity = ctx.Client.GetInteractivity();

            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            var emojiResult = await interactivity.WaitForReactionAsync(x => x.Message == errorMessage && (x.Emoji == helpEmoji));

            if (emojiResult.Result.Emoji == helpEmoji)
            {
                await SendHelpMessage(ctx);
            }
        }      
    }
}
