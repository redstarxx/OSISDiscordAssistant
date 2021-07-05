using System;
using System.Collections.Generic;
using System.Text;
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

namespace discordbot.Commands
{
    class ReminderCommandsModule : BaseCommandModule
    {
        private static readonly TimeSpan maxValue = TimeSpan.FromMilliseconds(int.MaxValue);

        //List<string> activeRemindersName = new List<string>();
        //List<string> activeRemindersDescription = new List<string>();

        [Command("remind")]
        public async Task Reminder(CommandContext ctx, string remindTarget, string timeSpan, params string[] remindMessage)
        {
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
                            string errorMessage = "**[ERROR]** Invalid reminder target.";
                            await ctx.RespondAsync(errorMessage).ConfigureAwait(false);
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

            if (timeSpan.Contains("/"))
            {
                DateTime currentTime = DateTime.Now;
                TimeSpan toCalculate;

                try
                {
                    toCalculate = DateTime.ParseExact(string.Join(" ", timeSpan), "dd/MM/yyyy", CultureInfo.CurrentCulture)
                        - currentTime;
                }

                catch
                {
                    try
                    {
                        toCalculate = DateTime.ParseExact(string.Join(" ", timeSpan), "MM/dd/yyyy", CultureInfo.CurrentCulture)
                            - currentTime;
                    }

                    catch
                    {
                        try
                        {
                            toCalculate = DateTime.ParseExact(string.Join(" ", timeSpan), "dd/MMM/yyyy", CultureInfo.CurrentCulture)
                                - currentTime;
                        }

                        catch
                        {
                            string errorMessage =
                                "**[ERROR]** An error occured while parsing your date time. Acceptable date time formats are " +
                                "`DD/MM/YYYY`, `MM/DD/YYYY` or `DD/MMM/YYYY`. \nExample: 25/06/2019, 06/25/2019, 25/JUN/2019.";
                            await ctx.RespondAsync(errorMessage).ConfigureAwait(false);
                            return;
                        }
                    }
                }

                // Checks whether the provided time span is not less than 30 seconds.
                if (toCalculate.TotalSeconds < 29)
                {
                    string errorMessage = "**[ERROR]** Minimum allowed time span is 30 seconds.";
                    await ctx.RespondAsync(errorMessage).ConfigureAwait(false);
                    return;
                }

                else
                {
                    double remainingMinutes = Math.Floor(toCalculate.TotalSeconds / 60);
                    double remainingHours = Math.Floor(remainingMinutes / 60);
                    double remainingDays = Math.Floor(remainingHours / 24);

                    string toSend = string.Empty;
                    StringBuilder stringBuilder = new StringBuilder(toSend);

                    if (remainingDays > 365)
                    {
                        string errorMessage = "**[ERROR]** Maximum allowed time span is one year.";
                        await ctx.RespondAsync(errorMessage).ConfigureAwait(false);
                        return;
                    }

                    if (remainingDays > 0.9)
                    {
                        int hoursRemainder = (int)((int)remainingHours - (remainingDays * 24));

                        if (hoursRemainder != 0)
                        {
                            toSend = $"Ok {ctx.Member.Mention}, in {remainingDays} day(s) and {hoursRemainder} hour(s) " +
                                $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                        }

                        else
                        {
                            toSend = $"Ok {ctx.Member.Mention}, in {remainingDays} day(s) " +
                                $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                        }
                    }

                    else if (remainingHours > 0.9)
                    {
                        int minutesRemainder = (int)((int)remainingMinutes - (remainingHours * 60));

                        if (minutesRemainder != 0)
                        {
                            toSend = $"Ok {ctx.Member.Mention}, in {remainingHours} hour(s) and {minutesRemainder} minute(s) " +
                                $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                        }

                        else
                        {
                            toSend = $"Ok {ctx.Member.Mention}, in {remainingHours} hour(s) " +
                                $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                        }
                    }

                    else if (remainingMinutes > 0.9)
                    {
                        int secondsRemainder = (int)((int)toCalculate.TotalSeconds - (remainingMinutes * 60));

                        if (secondsRemainder != 0)
                        {
                            toSend = $"Ok {ctx.Member.Mention}, in {remainingMinutes} minute(s) and {secondsRemainder} second(s) " +
                                $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                        }

                        else
                        {
                            toSend = $"Ok {ctx.Member.Mention}, in {remainingMinutes} minute(s) " +
                                $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                        }
                    }

                    else if (toCalculate.TotalSeconds < 59)
                    {
                        toSend = $"Ok {ctx.Member.Mention}, in {Math.Round(toCalculate.TotalSeconds)} seconds " +
                            $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                    }

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
                        await ctx.Channel.SendMessageAsync(reminder).ConfigureAwait(false);
                    });

                    reminderTask.Start();
                    await ctx.RespondAsync(toSend).ConfigureAwait(false);
                }
            }

            else
            {
                try
                {
                    TimeSpan remainingSeconds = ClientUtilities.ParseToSeconds(timeSpan);

                    // Checks whether the provided time span is not less than 30 seconds.
                    if (remainingSeconds.TotalSeconds < 29)
                    {
                        string errorMessage = "**[ERROR]** Minimum allowed time span is 30 seconds.";
                        await ctx.RespondAsync(errorMessage).ConfigureAwait(false);
                        return;
                    }

                    else
                    {
                        double remainingMinutes = Math.Floor(remainingSeconds.TotalSeconds / 60);
                        double remainingHours = Math.Floor(remainingMinutes / 60);
                        double remainingDays = Math.Floor(remainingHours / 24);

                        string toSend = string.Empty;
                        StringBuilder stringBuilder = new StringBuilder(toSend);

                        if (remainingDays > 365)
                        {
                            string errorMessage = "**[ERROR]** Maximum allowed time span is one year.";
                            await ctx.RespondAsync(errorMessage).ConfigureAwait(false);
                            return;
                        }

                        if (remainingDays > 0.9)
                        {
                            int hoursRemainder = (int)((int)remainingHours - (remainingDays * 24));

                            if (hoursRemainder != 0)
                            {
                                toSend = $"Ok {ctx.Member.Mention}, in {remainingDays} day(s) and {hoursRemainder} hour(s) " +
                                    $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                            }

                            else
                            {
                                toSend = $"Ok {ctx.Member.Mention}, in {remainingDays} day(s) " +
                                    $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                            }
                        }

                        else if (remainingHours > 0.9)
                        {
                            int minutesRemainder = (int)((int)remainingMinutes - (remainingHours * 60));

                            if (minutesRemainder != 0)
                            {
                                toSend = $"Ok {ctx.Member.Mention}, in {remainingHours} hour(s) and {minutesRemainder} minute(s) " +
                                    $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                            }

                            else
                            {
                                toSend = $"Ok {ctx.Member.Mention}, in {remainingHours} hour(s) " +
                                    $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                            }
                        }

                        else if (remainingMinutes > 0.9)
                        {
                            int secondsRemainder = (int)((int)remainingSeconds.TotalSeconds - (remainingMinutes * 60));

                            if (secondsRemainder != 0)
                            {
                                toSend = $"Ok {ctx.Member.Mention}, in {remainingMinutes} minute(s) and {secondsRemainder} second(s) " +
                                    $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                            }

                            else
                            {
                                toSend = $"Ok {ctx.Member.Mention}, in {remainingMinutes} minute(s) " +
                                    $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                            }
                        }

                        else if (remainingSeconds.TotalSeconds < 59)
                        {
                            toSend = $"Ok {ctx.Member.Mention}, in {Math.Round(remainingSeconds.TotalSeconds)} seconds " +
                                $"{youoreveryone} will be reminded of the following:\n\n {string.Join(" ", remindMessage)}";
                        }

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

                            long fullDelays = remainingSeconds.Ticks / maxValue.Ticks;
                            for (int i = 0; i < fullDelays; i++)
                            {
                                await Task.Delay(maxValue);
                                remainingSeconds -= maxValue;
                            }

                            await Task.Delay(remainingSeconds);
                            await ctx.Channel.SendMessageAsync(reminder).ConfigureAwait(false);
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
            var reminderInfoEmbed = new DiscordEmbedBuilder
            {
                Title = "OSIS DJUWITA BATAM — REMINDER FEATURE",
                Description = "Bot ini memiliki fitur mengingatkan seksi tertentu atau seluruh anggota OSIS untuk " +
                "berbagai kepentingan, seperti mengingatkan jadwal rapat atau hitung mundur jumlah hari menuju pelaksanaan event.\n\n" +
                "Berikut seksi-seksi yang dapat diingatkan oleh bot ini: \n• Inti (Inti OSIS)\n• Kesenian\n• Kewirausahaan\n" +
                "• IT (Informasi Teknologi)\n• Olahraga\n• Humas\n• Agama \nApabila ingin mengingatkan semua anggota, pilih `everyone`." +
                "\n\n**FORMAT PENGGUNAAN**\n`!remind [NAMA SEKSI / EVERYONE] [TANGGAL / WAKTU UNTUK DIINGATKAN (contoh: 25/06/2021 atau 6j30m)] [APA YANG INGIN DIINGATKAN]`\n" +
                "**CONTOH**\n`!remind kesenian 12j Upload poster event ke Instagram.`\n" +
                $"**HASIL**\nOke {ctx.User.Mention}, dalam 12 jam, seksi Kesenian akan diingatkan hal berikut:\n\n Upload poster event ke Instagram.",
                Timestamp = DateTime.Now.AddHours(7),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                }
            };

            await ctx.Message.DeleteAsync();
            await ctx.Member.SendMessageAsync(embed: reminderInfoEmbed).ConfigureAwait(false);
        }
    }
}
