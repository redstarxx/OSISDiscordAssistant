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
            await _reminderService.SendGuildReminders(null, ctx);
        }

        [SlashCommand("reminder_cancel", "Cancels a reminder from the specified ID, which is taken from /reminder_list.")]
        public async Task CancelReminderAsync(InteractionContext ctx, [Option("ID", "The ID of the reminder, which is taken from /reminder_list.")] long reminderId)
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
                $"{Formatter.Bold("/reminder_list")} - Lists all active reminders for this guild.\n" +
                $"{Formatter.Bold("/reminder_cancel")} - Cancels a reminder from the specified ID, which is taken from {Formatter.InlineCode("/reminder list")}.";

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
                        int mentionCount = 0;

                        foreach (char letter in remindTarget)
                        {
                            if (letter == '@')
                            {
                                mentionCount++;
                            }
                        }

                        if (mentionCount > 5)
                        {
                            await ctx.CreateResponseAsync($"{Formatter.Bold("[ERROR]")} You can remind up to 5 members and roles only!");

                            return;
                        }

                        displayTarget = remindTarget;

                        if (remindTarget.Contains(ctx.User.Id.ToString()))
                        {
                            displayTarget = "you";
                        }
                    }

                    // Expecting that the input would be a name of a member which belongs to the guild or a role...
                    else
                    {
                        var reminderTarget = await ClientUtilities.GetUserOrRoleMention(remindTarget, null, ctx);

                        if (reminderTarget is null)
                        {
                            // PENDING: more descriptive and helpful invalid reminder target message (similar to that of timespan embed)
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
                            await _reminderService.SendTimespanHelpEmbedAsync(null, ctx);

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

                    await _reminderService.RegisterReminder(remainingTime, targetChannel, remindMessage, remindTarget, displayTarget, null, ctx);
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

                await _reminderService.RegisterReminder(remainingTime, targetChannel, remindMessage, remindTarget, displayTarget, null, ctx);
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
                    await _reminderService.SendTimespanHelpEmbedAsync(null, ctx);

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

                    await _reminderService.RegisterReminder(remainingTime, targetChannel, remindMessage, remindTarget, displayTarget, null, ctx);
                }
            }
        }     
    }
}
