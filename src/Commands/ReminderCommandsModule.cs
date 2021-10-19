using System;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;
using DSharpPlus;
using Humanizer;
using OSISDiscordAssistant.Utilities;

namespace OSISDiscordAssistant.Commands
{
    class ReminderCommandsModule : BaseCommandModule
    {
        private static readonly TimeSpan maxValue = TimeSpan.FromMilliseconds(int.MaxValue);

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
                var errorMessage = await ctx.RespondAsync(toSend).ConfigureAwait(false);

                await SendHelpEmoji(ctx, errorMessage);

                return;
            }

            // Determines whether the user intends to remind themselves or @everyone.
            // Applies to the following two switch methods below.
            string mentionTarget = string.Empty;
            switch (remindTarget)
            {
                case "me":
                    mentionTarget = ctx.Member.Mention;
                    break;
                case "@everyone":
                    mentionTarget = "@everyone";
                    break;
                case "everyone":
                    mentionTarget = "@everyone";
                    break;
                default:
                    string toCheck = remindTarget.Remove(2);
                    if (remindTarget.StartsWith("<") && toCheck == "<@")
                    {
                        mentionTarget = remindTarget;
                    }

                    else
                    {
                        string toSend = $"{Formatter.Bold("[ERROR]")} Looks like an invalid reminder target! Type {Formatter.InlineCode("!remind")} to get help. Alternatively, click the emoji below to get help.";
                        var errorMessage = await ctx.RespondAsync(toSend).ConfigureAwait(false);

                        await SendHelpEmoji(ctx, errorMessage);

                        return;
                    }

                    break;
            }

            string youoreveryone = string.Empty;
            switch (remindTarget.ToLowerInvariant())
            {
                case "me":
                    youoreveryone = "you";
                    break;
                default:
                    youoreveryone = remindTarget;
                    break;
            }

            DiscordChannel targetChannel = remindChannel ?? ctx.Channel;

            if (timeSpan.Contains("/"))
            {
                DateTime currentTime = ClientUtilities.GetWesternIndonesianDateTime();
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
                            await ctx.RespondAsync(errorMessage).ConfigureAwait(false);

                            return;
                        }
                    }
                }

                TimeSpan remainingTime = remindTime - currentTime;

                // Checks whether the provided time span is not less than 30 seconds.
                if (remainingTime.TotalSeconds < 30)
                {
                    string errorMessage = "**[ERROR]** Minimum allowed time span is 30 seconds.";
                    await ctx.RespondAsync(errorMessage).ConfigureAwait(false);

                    return;
                }

                else
                {
                    if (remainingTime.Days > 365)
                    {
                        string errorMessage = "**[ERROR]** Maximum allowed time span is one year.";
                        await ctx.RespondAsync(errorMessage).ConfigureAwait(false);

                        return;
                    }

                    string toSend = $"Ok {ctx.Member.Mention}, in {remainingTime.Humanize(2)} ({Formatter.Timestamp(remindTime, TimestampFormat.LongDateTime)}) " +
                        $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";

                    string name = $"• {ctx.Member.DisplayName}#{ctx.Member.Discriminator} - {DateTime.Now}";

                    var reminderTask = new Task(async () =>
                    {
                        string reminder = string.Empty;
                        if (remindTarget == "me")
                        {
                            reminder = $"{DiscordEmoji.FromName(ctx.Client, ":alarm_clock:")} {ctx.Member.Mention}, " +
                            $"you wanted to be reminded of the following: \n\n{string.Join(" ", remindMessage)}";
                        }

                        else
                        {
                            reminder = $"{DiscordEmoji.FromName(ctx.Client, ":alarm_clock:")} {mentionTarget}, " +
                            $"{ctx.Member.Mention} wanted to remind you of the following: \n\n{string.Join(" ", remindMessage)}";
                        }

                        long fullDelays = remainingTime.Ticks / maxValue.Ticks;
                        for (int i = 0; i < fullDelays; i++)
                        {
                            await Task.Delay(maxValue);
                            remainingTime -= maxValue;
                        }

                        await Task.Delay(remainingTime);

                        if (targetChannel == ctx.Channel)
                        {
                            await ctx.RespondAsync(reminder).ConfigureAwait(false);
                        }

                        else
                        {
                            await targetChannel.SendMessageAsync(reminder).ConfigureAwait(false);
                        }
                    });

                    reminderTask.Start();

                    await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);                 
                }
            }

            else if (timeSpan.Contains(":"))
            {
                DateTime currentTime = ClientUtilities.GetWesternIndonesianDateTime();

                var remindTime = DateTime.ParseExact(timeSpan, "H:mm", null, DateTimeStyles.None);

                if (currentTime > remindTime)
                {
                    remindTime = remindTime.AddDays(1);
                }

                TimeSpan remainingTime = remindTime - currentTime;

                string name = $"• {ctx.Member.DisplayName}#{ctx.Member.Discriminator} - {DateTime.Now}";

                var reminderTask = new Task(async () =>
                {
                    string reminder = string.Empty;
                    if (remindTarget == "me")
                    {
                        reminder = $"{DiscordEmoji.FromName(ctx.Client, ":alarm_clock:")} {ctx.Member.Mention}, " +
                        $"you wanted to be reminded of the following: \n\n{string.Join(" ", remindMessage)}";
                    }

                    else
                    {
                        reminder = $"{DiscordEmoji.FromName(ctx.Client, ":alarm_clock:")} {mentionTarget}, " +
                        $"{ctx.Member.Mention} wanted to remind you of the following: \n\n{string.Join(" ", remindMessage)}";
                    }

                    long fullDelays = remainingTime.Ticks / maxValue.Ticks;
                    for (int i = 0; i < fullDelays; i++)
                    {
                        await Task.Delay(maxValue);
                        remainingTime -= maxValue;
                    }

                    await Task.Delay(remainingTime);

                    if (targetChannel == ctx.Channel)
                    {
                        await ctx.RespondAsync(reminder).ConfigureAwait(false);
                    }

                    else
                    {
                        await targetChannel.SendMessageAsync(reminder).ConfigureAwait(false);
                    }
                });

                reminderTask.Start();

                string toSend = null;

                if (remindTime.ToShortDateString() != currentTime.ToShortDateString())
                {
                    toSend = $"Ok {ctx.Member.Mention}, tomorrow, in {remainingTime.Humanize(2)} ({Formatter.Timestamp(remindTime, TimestampFormat.LongDateTime)}) {youoreveryone} will be reminded of the following:\n\n" +
                    $" {string.Join(" ", remindMessage)}";
                }

                else
                {
                    toSend = $"Ok {ctx.Member.Mention}, in {remainingTime.Humanize(2)} ({Formatter.Timestamp(remindTime, TimestampFormat.LongDateTime)}) {youoreveryone} will be reminded of the following:\n\n" +
                    $" {string.Join(" ", remindMessage)}";
                }

                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
            }

            else
            {
                try
                {
                    DateTime currentTime = ClientUtilities.GetWesternIndonesianDateTime();

                    TimeSpan remainingTime = ClientUtilities.ParseToSeconds(timeSpan);

                    DateTime remindTime = currentTime + remainingTime;

                    // Checks whether the provided time span is not less than 30 seconds.
                    if (remainingTime.TotalSeconds < 30)
                    {
                        string errorMessage = "**[ERROR]** Minimum allowed time span is 30 seconds.";
                        await ctx.RespondAsync(errorMessage).ConfigureAwait(false);

                        return;
                    }

                    else
                    {
                        if (remainingTime.Days > 365)
                        {
                            string errorMessage = "**[ERROR]** Maximum allowed time span is one year.";
                            await ctx.RespondAsync(errorMessage).ConfigureAwait(false);

                            return;
                        }

                        string toSend = $"Ok {ctx.Member.Mention}, in {remainingTime.Humanize(2)} ({Formatter.Timestamp(remindTime, TimestampFormat.LongDateTime)}) " +
                            $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";

                        string name = $"• {ctx.Member.DisplayName}#{ctx.Member.Discriminator} - {DateTime.Now}";

                        var reminderTask = new Task(async () =>
                        {
                            string reminder = string.Empty;
                            if (remindTarget == "me")
                            {
                                reminder = $"{DiscordEmoji.FromName(ctx.Client, ":alarm_clock:")} {ctx.Member.Mention}, " +
                                $"you wanted to be reminded of the following: \n\n{string.Join(" ", remindMessage)}";
                            }

                            else
                            {
                                reminder = $"{DiscordEmoji.FromName(ctx.Client, ":alarm_clock:")} {mentionTarget}, " +
                                $"{ctx.Member.Mention} wanted to remind you of the following: \n\n{string.Join(" ", remindMessage)}";
                            }

                            long fullDelays = remainingTime.Ticks / maxValue.Ticks;
                            for (int i = 0; i < fullDelays; i++)
                            {
                                await Task.Delay(maxValue);
                                remainingTime -= maxValue;
                            }

                            await Task.Delay(remainingTime);

                            if (targetChannel == ctx.Channel)
                            {
                                await ctx.RespondAsync(reminder).ConfigureAwait(false);
                            }

                            else
                            {
                                await targetChannel.SendMessageAsync(reminder).ConfigureAwait(false);
                            }
                        });

                        reminderTask.Start();

                        await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);                        
                    }
                }

                catch
                {
                    var errorMessage = await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} An error occured. Have you tried to use the command correctly? Type {Formatter.InlineCode("!remind")} to get help. Alternatively, click the emoji below to get help.").ConfigureAwait(false);

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

            await errorMessage.CreateReactionAsync(helpEmoji).ConfigureAwait(false);

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
