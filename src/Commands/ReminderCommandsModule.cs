﻿using System;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Entities;
using DSharpPlus;
using OSISDiscordAssistant.Services;
using OSISDiscordAssistant.Utilities;
using OSISDiscordAssistant.Models;
using OSISDiscordAssistant.Entities;
using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace OSISDiscordAssistant.Commands
{
    class ReminderCommandsModule : BaseCommandModule
    {
        private readonly ReminderContext _reminderContext;
        private readonly IReminderService _reminderService;

        public ReminderCommandsModule(ReminderContext reminderContext, IReminderService reminderService)
        {
            _reminderContext = reminderContext;
            _reminderService = reminderService;
        }

        [Command("reminders")]
        public async Task GetGuildRemindersAsync(CommandContext ctx)
        {
            await SendGuildReminders(ctx);
        }

        [Command("reminder")]
        public async Task GetGuildRemindersAsync(CommandContext ctx, string option)
        {
            if (option is "list")
            {
                await SendGuildReminders(ctx);
            }

            else if (option is "cancel")
            {
                await ctx.RespondAsync($"{Formatter.Bold("[SYNTAX]")} osis reminder cancel [REMINDER ID (refer to {Formatter.InlineCode("osis reminder list")})]\nExample: {Formatter.InlineCode("osis reminder cancel 25")}");
            }

            else
            {
                await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} The option {Formatter.InlineCode(option)} does not exist. Type {Formatter.InlineCode("osis reminder")} to view all possible actions.");
            }
        }

        [Command("reminder")]
        public async Task ReminderOptionsAsync(CommandContext ctx, string option, [RemainingText] string id)
        {
            if (option is "cancel")
            {
                bool isNumber = int.TryParse(id, out int rowID);

                if (!isNumber)
                {
                    await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} You need to provide the ID of the reminder, which is a number. Refer to {Formatter.InlineCode("reminder list")}");

                    return;
                }

                var reminder = _reminderContext.Reminders.FirstOrDefault(x => x.Id == rowID);

                if (reminder is null)
                {
                    await ctx.RespondAsync($"Reminder ID {Convert.ToInt32(id)} does not exist.");

                    return;
                }

                if (reminder.Cancelled is false)
                {
                    reminder.Cancelled = true;

                    await _reminderContext.SaveChangesAsync();

                    await ctx.RespondAsync($"Reminder ID {reminder.Id} is now cancelled.");
                }

                else
                {
                    await ctx.RespondAsync($"Reminder ID {reminder.Id} has already been cancelled.");
                }
            }

            else
            {
                await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} The option {Formatter.InlineCode(option)} does not exist. Type {Formatter.InlineCode("osis reminder")} to view all possible actions.");
            }
        }

        [Command("reminder")]
        public async Task ReminderOptionsAsync(CommandContext ctx)
        {
            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = "Reminders Manager - Overview",
                Timestamp = DateTime.Now,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                },
                Color = DiscordColor.MidnightBlue
            };

            embedBuilder.Description = $"{Formatter.Bold("osis remind")} - Creates a new reminder.\n" +
                $"{Formatter.Bold("osis reminders")} / {Formatter.Bold("osis reminder list")} - Lists all active reminders for this guild.\n" +
                $"{Formatter.Bold("osis reminder cancel")} - Cancels a reminder from the specified ID, which is taken from {Formatter.InlineCode("osis reminder list")}.\n";

            await ctx.Channel.SendMessageAsync(embed: embedBuilder);
        }

        [Command("remind")]
        public async Task RemindWithChannelAsync(CommandContext ctx, string remindTarget, string timeSpan, DiscordChannel targetChannel, [RemainingText] string remindMessage)
        {
            await CreateReminderAsync(ctx, remindTarget, timeSpan, remindMessage, targetChannel);
        }

        [Command("remind")]
        public async Task RemindWithoutChannelAsync(CommandContext ctx, string remindTarget, string timeSpan, [RemainingText] string remindMessage)
        {
            await CreateReminderAsync(ctx, remindTarget, timeSpan, remindMessage, null);
        }

        /// <summary>
        /// Creates a reminder which is based from creating a delayed task that sends a message after delaying the task for the specified amount of time.
        /// </summary>
        /// <returns>A reminder task that runs in the background.</returns>
        public async Task CreateReminderAsync(CommandContext ctx, string remindTarget, string timeSpan, string remindMessage, DiscordChannel remindChannel = null)
        {
            if (remindMessage.Length == 0)
            {
                var errorMessage = await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} You cannot remind someone with an empty message. Type {Formatter.InlineCode("osis remind")} to get help. Alternatively, click the emoji below to get help.");

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
                    if (remindTarget.Remove(2) == "<@")
                    {
                        displayTarget = remindTarget;

                        if (remindTarget == ctx.User.Mention)
                        {
                            displayTarget = "you";
                        }
                    }

                    // Expecting that the input would be a name of a member which belongs to the guild or a role...
                    else
                    {
                        var reminderTarget = await GetUserOrRoleMention(ctx, remindTarget);

                        if (reminderTarget is null)
                        {
                            var errorMessage = await ctx.RespondAsync($"{Formatter.Bold("[ERROR]")} Looks like an invalid reminder target! Type {Formatter.InlineCode("osis remind")} to ensure you are following the syntax correctly.");

                            await SendHelpEmoji(ctx, errorMessage);

                            break;
                        }

                        remindTarget = reminderTarget.MentionString;

                        string userDisplayName = reminderTarget.DisplayName is null ? string.Empty : $"({reminderTarget.DisplayName})";

                        displayTarget = reminderTarget.IsUser is true ? $"{reminderTarget.Name}#{reminderTarget.Discriminator} {userDisplayName}" : $"everyone with the role {Formatter.Bold(reminderTarget.Name)}";
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
                                $"{Formatter.Bold("[ERROR]")} An error occured while parsing your date. Acceptable date formats are " +
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
                    string errorMessage = $"{Formatter.Bold("[ERROR]")} Minimum allowed time span is 30 seconds.";
                    await ctx.RespondAsync(errorMessage);

                    return;
                }

                else
                {
                    if (remainingTime.Days > 365)
                    {
                        string errorMessage = $"{Formatter.Bold("[ERROR]")} Maximum allowed time span is one year.";
                        await ctx.RespondAsync(errorMessage);

                        return;
                    }

                    await RegisterReminder(remainingTime, targetChannel, remindMessage, ctx, remindTarget, displayTarget);
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

                await RegisterReminder(remainingTime, targetChannel, remindMessage, ctx, remindTarget, displayTarget);
            }

            else
            {
                TimeSpan remainingTime = ClientUtilities.ParseToSeconds(timeSpan);

                DateTime remindTime = currentTime + remainingTime;

                // Checks whether the provided time span is not less than 30 seconds.
                if (remainingTime.TotalSeconds < 30)
                {
                    string errorMessage = $"{Formatter.Bold("[ERROR]")} Minimum allowed time span is 30 seconds.";
                    await ctx.RespondAsync(errorMessage);

                    return;
                }

                else
                {
                    if (remainingTime.Days > 365)
                    {
                        string errorMessage = $"{Formatter.Bold("[ERROR]")} Maximum allowed time span is one year.";
                        await ctx.RespondAsync(errorMessage);

                        return;
                    }

                    await RegisterReminder(remainingTime, targetChannel, remindMessage, ctx, remindTarget, displayTarget);
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

        /// <summary>
        /// Stores the reminder into the database and dispatches the reminder. It will check whether the reminder was cancelled before the reminder content is to be sent.
        /// </summary>
        private async Task RegisterReminder(TimeSpan remainingTime, DiscordChannel targetChannel, string remindMessage, CommandContext ctx, string remindTarget, string displayTarget)
        {
            var reminder = new Reminder()
            {
                InitiatingUserId = ctx.Member.Id,
                TargetedUserOrRoleMention = remindTarget,
                UnixTimestampRemindAt = ClientUtilities.ConvertDateTimeToUnixTimestamp(DateTime.UtcNow.Add(remainingTime)),
                TargetGuildId = targetChannel.Guild.Id,
                ReplyMessageId = ctx.Message.ReferencedMessage is null ? ctx.Message.Id : ctx.Message.ReferencedMessage.Id,
                TargetChannelId = targetChannel.Id,
                Cancelled = false,
                Content = remindMessage
            };

            if (ctx.Channel != targetChannel)
            {
                reminder.ReplyMessageId = ctx.Message.Id;
            }

            _reminderContext.Add(reminder);

            await _reminderContext.SaveChangesAsync();

            _reminderService.CreateReminderTask(reminder, remainingTime);

            await ctx.Channel.SendMessageAsync(CreateReminderReceiptMessage(remainingTime, remindMessage, displayTarget));
        }

        /// <summary>
        /// Composes a reminder receipt message (sent upon the completion of firing a reminder task).
        /// </summary>
        /// <param name="timeSpan">The timespan object.</param>
        /// <param name="remindMessage">Something to remind (text, link, picture, whatever).</param>
        /// <param name="displayTarget"></param>
        /// <returns></returns>
        private string CreateReminderReceiptMessage(TimeSpan timeSpan, string remindMessage, string displayTarget)
        {
            string receiptMessage = $"Okay! In {timeSpan.Humanize(1)} ({Formatter.Timestamp(timeSpan, TimestampFormat.LongDateTime)}) {displayTarget} will be reminded of the following:\n\n {remindMessage}";

            return receiptMessage.Replace("  ", " ");
        }

        /// <summary>
        /// Sends the list of active reminders that belongs to the respective guild.
        /// </summary>
        /// <returns></returns>
        private async Task SendGuildReminders(CommandContext ctx)
        {
            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = $"Listing All Reminders for {ctx.Guild.Name}...",
                Description = $"To create a new reminder or cancel an active reminder, use {Formatter.Bold("osis reminder")}.",
                Timestamp = DateTime.Now,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                },
                Color = DiscordColor.MidnightBlue
            };

            var reminders = _reminderContext.Reminders.AsNoTracking().Where(x => x.Cancelled == false).Where(x => x.TargetGuildId == ctx.Guild.Id);

            foreach (var reminder in reminders)
            {
                var initiatingUser = await ctx.Guild.GetMemberAsync(reminder.InitiatingUserId);

                embedBuilder.AddField($"(ID: {reminder.Id}) by {initiatingUser.Username}#{initiatingUser.Discriminator} ({initiatingUser.DisplayName})", $"When: {Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(reminder.UnixTimestampRemindAt), TimestampFormat.LongDateTime)} ({Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(reminder.UnixTimestampRemindAt), TimestampFormat.RelativeTime)})\nWho: {reminder.TargetedUserOrRoleMention}\nContent: {reminder.Content}", true);
            }

            _ = reminders.Count() is 0 ? await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} There are no reminders to display for this guild.") : await ctx.Channel.SendMessageAsync(embedBuilder.Build());
        }

        /// <summary>
        /// Gets the mention string of the partial member or role name.
        /// </summary>
        /// <returns>A <see cref="ReminderTarget" /> object which contains the mention string and necessary data of the associated member if any contains the given string, or the role mention string if fetching the associated member returns null and a role with the name that contains the given string. Returns null if no member or role name contains the given string.</returns>
        private async Task<ReminderTarget> GetUserOrRoleMention(CommandContext ctx, string remindTarget)
        {
            var list = await ctx.Guild.GetAllMembersAsync();
            var user = list.FirstOrDefault(x => x.Username.ToLowerInvariant().Replace(" ", "").Contains(remindTarget.ToLowerInvariant())) ?? list.FirstOrDefault(x => x.DisplayName.ToLowerInvariant().Replace(" ", "").Contains(remindTarget.ToLowerInvariant()));

            if (user is null)
            {
                var role = ctx.Guild.Roles.Values.FirstOrDefault(x => x.Name.ToLowerInvariant().Replace(" ", "").Contains(remindTarget.ToLowerInvariant()));

                if (role is null)
                {
                    return null;
                }

                else
                {
                    return new ReminderTarget()
                    {
                        Name = role.Name,
                        MentionString = role.Mention,
                        IsUser = false
                    };
                }
            }

            else
            {
                return new ReminderTarget()
                {
                    Name = user.Username,
                    DisplayName = user.DisplayName == user.Username ? null : user.DisplayName,
                    Discriminator = user.Discriminator,
                    MentionString = user.Mention,
                    IsUser = true
                };
            }
        }

        private async Task SendHelpMessage(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"{Formatter.Bold("[SYNTAX]")} osis remind [ROLE / MEMBER (accepts one recipient only, by mentioning or type the partial name or the whole name without space)] [DATE / TIME TO REMIND (example: 25/06/2021 or 6h30m or 12:30 or 30m)] [CHANNEL (optional)] [MESSAGE]\nExample: {Formatter.InlineCode("osis remind everyone 2h VC meeting")} or {Formatter.InlineCode("osis remind SecurityGuards 1d Ban DM raiders")}");
        }

        private async Task SendHelpEmoji(CommandContext ctx, DiscordMessage errorMessage)
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
