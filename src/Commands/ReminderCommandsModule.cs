using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.Net.Models;
using System.Linq;
using DSharpPlus.EventArgs;
using System.Reflection;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;
using System.Globalization;
using Humanizer;

namespace discordbot.Commands
{
    class ReminderCommandsModule : BaseCommandModule
    {
        private static readonly TimeSpan maxValue = TimeSpan.FromMilliseconds(int.MaxValue);

        [Command("remind")]
        public async Task RemindWithChannel(CommandContext ctx, string remindTarget, string timeSpan, DiscordChannel toChannel, params string[] toRemind)
        {
            await CreateReminder(ctx, remindTarget, timeSpan, toChannel, toRemind);
        }

        [Command("remind")]
        public async Task RemindWithoutChannel(CommandContext ctx, string remindTarget, string timeSpan, params string[] toRemind)
        {
            await CreateReminder(ctx, remindTarget, timeSpan, null, toRemind);
        }

        /// <summary>
        /// Creates a reminder which is based from creating a delayed task that sends a message after delaying the task for the specified amount of time.
        /// </summary>
        /// <returns>A reminder task that runs in the background.</returns>
        public async Task CreateReminder(CommandContext ctx, string remindTarget, string timeSpan, DiscordChannel remindChannel = null, params string[] toRemind)
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
                case "everyone":
                    mentionTarget = "@everyone";
                    break;
                default:
                    if (ClientUtilities.GetRoleID(remindTarget) != 0)
                    {
                        var divisionalRole = ctx.Guild.GetRole(ClientUtilities.GetRoleID(remindTarget));
                        mentionTarget = divisionalRole.Mention;
                    }

                    else
                    {
                        string toCheck = remindTarget.Remove(2);
                        if (remindTarget.StartsWith("<") && toCheck == "<@")
                        {
                            mentionTarget = remindTarget;
                        }

                        else
                        {
                            string toSend = $"{Formatter.Bold("[ERROR]")} Invalid reminder target. Type {Formatter.InlineCode("!remind")} to get help. Alternatively, click the emoji below to get help.";
                            var errorMessage = await ctx.RespondAsync(toSend).ConfigureAwait(false);

                            await SendHelpEmoji(ctx, errorMessage);

                            return;
                        }
                    }

                    break;
            }

            string youoreveryone = string.Empty;
            switch (remindTarget.ToLowerInvariant())
            {
                case "me":
                    youoreveryone = "you";
                    break;
                case "everyone":
                    youoreveryone = "everyone";
                    break;
                case "inti":
                    youoreveryone = "Inti OSIS members";
                    break;
                case "it":
                    youoreveryone = "Seksi Informasi Teknologi members";
                    break;
                case "kesenian":
                    youoreveryone = "Seksi Kesenian members";
                    break;
                case "kewirausahaan":
                    youoreveryone = "Seksi Kewirausahaan members";
                    break;
                case "olahraga":
                    youoreveryone = "Seksi Olahraga members";
                    break;
                case "humas":
                    youoreveryone = "Seksi Humas members";
                    break;
                case "agama":
                    youoreveryone = "Seksi Agama members";
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

                TimeSpan toCalculate = remindTime - currentTime;

                // Checks whether the provided time span is not less than 30 seconds.
                if (toCalculate.TotalSeconds < 30)
                {
                    string errorMessage = "**[ERROR]** Minimum allowed time span is 30 seconds.";
                    await ctx.RespondAsync(errorMessage).ConfigureAwait(false);

                    return;
                }

                else
                {
                    if (toCalculate.Days > 365)
                    {
                        string errorMessage = "**[ERROR]** Maximum allowed time span is one year.";
                        await ctx.RespondAsync(errorMessage).ConfigureAwait(false);

                        return;
                    }

                    string toSend = $"Ok {ctx.Member.Mention}, in {toCalculate.Humanize(2)} ({remindTime.ToString()}) " +
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

                        long fullDelays = toCalculate.Ticks / maxValue.Ticks;
                        for (int i = 0; i < fullDelays; i++)
                        {
                            await Task.Delay(maxValue);
                            toCalculate -= maxValue;
                        }

                        await Task.Delay(toCalculate);
                        await targetChannel.SendMessageAsync(reminder).ConfigureAwait(false);
                    });

                    reminderTask.Start();

                    await ctx.RespondAsync(toSend).ConfigureAwait(false);                 
                }
            }

            else if (timeSpan.Contains(":"))
            {
                DateTime currentTime = ClientUtilities.GetWesternIndonesianDateTime();

                var toParse = DateTime.ParseExact(timeSpan, "H:mm", null, DateTimeStyles.None);

                if (currentTime > toParse)
                {
                    toParse = toParse.AddDays(1);
                }

                TimeSpan time = toParse - currentTime;

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

                    long fullDelays = time.Ticks / maxValue.Ticks;
                    for (int i = 0; i < fullDelays; i++)
                    {
                        await Task.Delay(maxValue);
                        time -= maxValue;
                    }

                    await Task.Delay(time);
                    await targetChannel.SendMessageAsync(reminder).ConfigureAwait(false);
                });

                reminderTask.Start();

                string toSend = null;

                if (toParse.ToShortDateString() != currentTime.ToShortDateString())
                {
                    toSend = $"Ok {ctx.Member.Mention}, tomorrow, in {time.Humanize(2)} ({toParse.ToString()}) {youoreveryone} will be reminded of the following:\n\n" +
                    $" {string.Join(" ", remindMessage)}";
                }

                else
                {
                    toSend = $"Ok {ctx.Member.Mention}, in {time.Humanize(2)} ({toParse.ToString()}) {youoreveryone} will be reminded of the following:\n\n" +
                    $" {string.Join(" ", remindMessage)}";
                }

                await ctx.Channel.SendMessageAsync(toSend).ConfigureAwait(false);
            }

            else
            {
                try
                {
                    DateTime currentTime = ClientUtilities.GetWesternIndonesianDateTime();

                    TimeSpan toCalculate = ClientUtilities.ParseToSeconds(timeSpan);

                    DateTime remainingDateTime = currentTime + toCalculate;

                    // Checks whether the provided time span is not less than 30 seconds.
                    if (toCalculate.TotalSeconds < 30)
                    {
                        string errorMessage = "**[ERROR]** Minimum allowed time span is 30 seconds.";
                        await ctx.RespondAsync(errorMessage).ConfigureAwait(false);

                        return;
                    }

                    else
                    {
                        if (toCalculate.Days > 365)
                        {
                            string errorMessage = "**[ERROR]** Maximum allowed time span is one year.";
                            await ctx.RespondAsync(errorMessage).ConfigureAwait(false);

                            return;
                        }

                        string toSend = $"Ok {ctx.Member.Mention}, in {toCalculate.Humanize(2)} ({remainingDateTime.ToString()}) " +
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

                            long fullDelays = toCalculate.Ticks / maxValue.Ticks;
                            for (int i = 0; i < fullDelays; i++)
                            {
                                await Task.Delay(maxValue);
                                toCalculate -= maxValue;
                            }

                            await Task.Delay(toCalculate);
                            await targetChannel.SendMessageAsync(reminder).ConfigureAwait(false);                
                        });

                        reminderTask.Start();

                        await ctx.RespondAsync(toSend).ConfigureAwait(false);                        
                    }
                }

                catch
                {
                    string errorMessage = "**[ERROR]** An error occured. Have you tried to use the command correctly?";
                    await ctx.RespondAsync(errorMessage).ConfigureAwait(false);
                }
            }
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        [Command("remind")]
        public async Task Reminder(CommandContext ctx)
        {
            await SendHelpEmbed (ctx, true);
        }

        public async Task SendHelpEmoji(CommandContext ctx, DiscordMessage errorMessage)
        {
            var helpEmoji = DiscordEmoji.FromName(ctx.Client, ":sos:");

            await errorMessage.CreateReactionAsync(helpEmoji).ConfigureAwait(false);

            var interactivity = ctx.Client.GetInteractivity();

            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            var emojiResult = await interactivity.WaitForReactionAsync(x => x.Message == errorMessage && (x.Emoji == helpEmoji));

            if (emojiResult.Result.Emoji == helpEmoji)
            {
                await SendHelpEmbed(ctx, false);
            }
        }

        public async Task SendHelpEmbed(CommandContext ctx, bool removeOriginalMessage)
        {
            var reminderInfoEmbed = new DiscordEmbedBuilder
            {
                Title = "OSIS DJUWITA BATAM — REMINDER FEATURE",
                Description = "Bot ini memiliki fitur mengingatkan seksi tertentu atau seluruh anggota OSIS untuk " +
                "berbagai kepentingan, seperti mengingatkan jadwal rapat atau hitung mundur jumlah hari menuju pelaksanaan event.\n\n" +
                "Berikut seksi-seksi yang dapat diingatkan oleh bot ini: \n• Inti (Inti OSIS)\n• Kesenian\n• Kewirausahaan\n" +
                "• IT (Informasi Teknologi)\n• Olahraga\n• Humas\n• Agama \nApabila ingin mengingatkan semua anggota, pilih `everyone` atau dengan langsung mention role yang diinginkan." +
                "\n\n**FORMAT PENGGUNAAN**\n`!remind [NAMA SEKSI / MENTION ROLE / EVERYONE] [TANGGAL / WAKTU UNTUK DIINGATKAN (contoh: 25/06/2021 atau 6j30m atau 12:30)] [CHANNEL (opsional)] [APA YANG INGIN DIINGATKAN]`\n" +
                "**CONTOH**\n`!remind kesenian 12:30 Upload poster event ke Instagram.`\n" +
                $"**HASIL**\nOke {ctx.User.Mention}, dalam 12 jam, seksi Kesenian akan diingatkan hal berikut:\n\n Upload poster event ke Instagram.",
                Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                },
                Color = DiscordColor.MidnightBlue
            };

            if (removeOriginalMessage)
            {
                await ctx.Message.DeleteAsync();
            }

            await ctx.Member.SendMessageAsync(embed: reminderInfoEmbed).ConfigureAwait(false);
        }
    }
}
