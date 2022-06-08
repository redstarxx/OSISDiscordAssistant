using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus;
using OSISDiscordAssistant.Services;
using OSISDiscordAssistant.Utilities;
using OSISDiscordAssistant.Models;
using OSISDiscordAssistant.Entities;
using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace OSISDiscordAssistant.Commands
{
    class ReminderSlashCommandsModule : ApplicationCommandModule
    {
        private readonly ReminderContext _reminderContext;
        private readonly IReminderService _reminderService;

        public ReminderSlashCommandsModule(ReminderContext reminderContext, IReminderService reminderService)
        {
            _reminderContext = reminderContext;
            _reminderService = reminderService;
        }

        [SlashCommand("reminder_list", "View all reminders for this guild.")]
        public async Task GetGuildRemindersAsync(InteractionContext ctx)
        {
            await SendGuildReminders(ctx);
        }

        [SlashCommand("reminder_cancel", "Cancels a reminder from the specified ID, which is taken from /reminder_list.")]
        public async Task CancelReminderAsync(InteractionContext ctx, [Option("ID", "The ID of the reminder, which is taken from /reminder list.")] long reminderId)
        {
            var reminder = _reminderContext.Reminders.FirstOrDefault(x => x.Id == reminderId);

            if (reminder is null)
            {
                await ctx.CreateResponseAsync($"Reminder ID {Convert.ToInt32(reminderId)} does not exist.");

                return;
            }

            if (reminder.Cancelled is false)
            {
                reminder.Cancelled = true;

                await _reminderContext.SaveChangesAsync();

                await ctx.CreateResponseAsync($"Reminder ID {reminder.Id} is now cancelled.");
            }

            else
            {
                await ctx.CreateResponseAsync($"Reminder ID {reminder.Id} has already been cancelled.");
            }
        }

        [SlashCommand("reminder", "View commands to manage or list upcoming reminders.")]
        public async Task ReminderOptionsAsync(InteractionContext ctx)
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

            embedBuilder.Description = $"{Formatter.Bold("/remind")} - Creates a new reminder.\n" +
                $"{Formatter.Bold("/reminder list")} - Lists all active reminders for this guild.\n" +
                $"{Formatter.Bold("/reminder cancel")} - Cancels a reminder from the specified ID, which is taken from {Formatter.InlineCode("/reminder list")}.";

            await ctx.CreateResponseAsync(embed: embedBuilder);
        }

        [SlashCommand("remind", "Remind yourself, a role, a user, or everyone about something.")]
        public async Task RemindWithoutChannelAsync(InteractionContext ctx, [Option("target", "Type the partial or full name of the member or role you want to remind, mentions are also welcome.")] string remindTarget, [Option("timespan", "Date / time when to remind (example: 25/06/2021 or 6h30m or 12:30 or 30m).")] string timeSpan, [Option("message", "What would you like to remind?")] string remindMessage)
        {
            await CreateReminderAsync(ctx, remindTarget, timeSpan, remindMessage, null);
        }

        /// <summary>
        /// Creates a reminder which is based from creating a delayed task that sends a message after delaying the task for the specified amount of time.
        /// </summary>
        /// <returns>A reminder task that runs in the background.</returns>
        public async Task CreateReminderAsync(InteractionContext ctx, string remindTarget, string timeSpan, string remindMessage, DiscordChannel remindChannel = null)
        {
            if (remindMessage.Length == 0)
            {
                await ctx.CreateResponseAsync($"{Formatter.Bold("[ERROR]")} You cannot remind someone with an empty message.");

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
                            await ctx.CreateResponseAsync($"{Formatter.Bold("[ERROR]")} Looks like an invalid reminder target! Type {Formatter.InlineCode("/remind")} to ensure you are following the syntax correctly.");

                            return;
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
                            await SendTimespanHelpEmbedAsync(ctx);

                            return;
                        }
                    }
                }

                TimeSpan remainingTime = remindTime - currentTime;

                // Checks whether the provided time span is not less than 30 seconds.
                if (remainingTime.TotalSeconds < 30)
                {
                    string errorMessage = $"{Formatter.Bold("[ERROR]")} Minimum allowed time span is 30 seconds.";
                    await ctx.CreateResponseAsync(errorMessage);

                    return;
                }

                else
                {
                    if (remainingTime.Days > 365)
                    {
                        string errorMessage = $"{Formatter.Bold("[ERROR]")} Maximum allowed time span is one year.";
                        await ctx.CreateResponseAsync(errorMessage);

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
                TimeSpan remainingTime = TimeSpan.Zero;

                try
                {
                    remainingTime = ClientUtilities.ParseToSeconds(timeSpan);
                }

                catch
                {
                    await SendTimespanHelpEmbedAsync(ctx);

                    return;
                }

                DateTime remindTime = currentTime + remainingTime;

                // Checks whether the provided time span is not less than 30 seconds.
                if (remainingTime.TotalSeconds < 30)
                {
                    string errorMessage = $"{Formatter.Bold("[ERROR]")} Minimum allowed time span is 30 seconds.";
                    await ctx.CreateResponseAsync(errorMessage);

                    return;
                }

                else
                {
                    if (remainingTime.Days > 365)
                    {
                        string errorMessage = $"{Formatter.Bold("[ERROR]")} Maximum allowed time span is one year.";
                        await ctx.CreateResponseAsync(errorMessage);

                        return;
                    }

                    await RegisterReminder(remainingTime, targetChannel, remindMessage, ctx, remindTarget, displayTarget);
                }
            }
        }

        // ----------------------------------------------------------
        // COMMAND HELPERS BELOW
        // ----------------------------------------------------------

        /// <summary>
        /// Stores the reminder into the database and dispatches the reminder. It will check whether the reminder was cancelled before the reminder content is to be sent.
        /// </summary>
        private async Task RegisterReminder(TimeSpan remainingTime, DiscordChannel targetChannel, string remindMessage, InteractionContext ctx, string remindTarget, string displayTarget)
        {
            var reminder = new Reminder()
            {
                InitiatingUserId = ctx.Member.Id,
                TargetedUserOrRoleMention = remindTarget,
                UnixTimestampRemindAt = ClientUtilities.ConvertDateTimeToUnixTimestamp(DateTime.UtcNow.Add(remainingTime)),
                TargetGuildId = targetChannel.Guild.Id,
                //ReplyMessageId = ctx.Message.ReferencedMessage is null ? ctx.Message.Id : ctx.Message.ReferencedMessage.Id,
                TargetChannelId = targetChannel.Id,
                Cancelled = false,
                Content = remindMessage
            };

            //if (ctx.Channel != targetChannel)
            //{
            //    reminder.ReplyMessageId = ctx.Message.Id;
            //}

            _reminderContext.Add(reminder);

            await _reminderContext.SaveChangesAsync();

            _reminderService.CreateReminderTask(reminder, remainingTime);

            await ctx.CreateResponseAsync(CreateReminderReceiptMessage(remainingTime, remindMessage, displayTarget));
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
        private async Task SendGuildReminders(InteractionContext ctx)
        {
            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = $"Listing All Reminders for {ctx.Guild.Name}...",
                Description = $"To view commands to manage or list upcoming reminders, use {Formatter.Bold("/reminder")}.",
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

            if (reminders.Count() is 0)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{Formatter.Bold("[ERROR]")} There are no reminders to display for this guild."));
            }

            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embedBuilder.Build()));
            }
        }

        /// <summary>
        /// Gets the mention string of the partial member or role name.
        /// </summary>
        /// <returns>A <see cref="ReminderTarget" /> object which contains the mention string and necessary data of the associated member if any contains the given string, or the role mention string if fetching the associated member returns null and a role with the name that contains the given string. Returns null if no member or role name contains the given string.</returns>
        private async Task<ReminderTarget> GetUserOrRoleMention(InteractionContext ctx, string remindTarget)
        {
            var list = await ctx.Guild.GetAllMembersAsync();
            var user = list.FirstOrDefault(x => x.Username.ToLowerInvariant().Contains(remindTarget.ToLowerInvariant())) ?? list.FirstOrDefault(x => x.DisplayName.ToLowerInvariant().Contains(remindTarget.ToLowerInvariant()));

            if (user is null)
            {
                var role = ctx.Guild.Roles.Values.FirstOrDefault(x => x.Name.ToLowerInvariant().Contains(remindTarget.ToLowerInvariant()));

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

        private async Task SendTimespanHelpEmbedAsync(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
            {
                Title = "Invalid timespan value given!",
                Description = $"At the moment, I can only accept three forms of timespan, which is:\n• Shortened dates, example: {Formatter.InlineCode("25/06/2022")} or {Formatter.InlineCode("25/JUNE/2022")},\n• Relative time, example: {Formatter.InlineCode("2h")} to remind you in two hours or {Formatter.InlineCode("12h30m")} in twelve hours and 30 minutes,\n• Or you can just point out the time you want the reminder to be sent if you are going to remind them within the next 24 hours, example: {Formatter.InlineCode("11:30")}. Follow the 24 hours format when using this option.",
                Timestamp = DateTime.Now,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                },
                Color = DiscordColor.MidnightBlue
            });
        }
    }
}
