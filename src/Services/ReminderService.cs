using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OSISDiscordAssistant.Constants;
using OSISDiscordAssistant.Models;
using OSISDiscordAssistant.Utilities;

namespace OSISDiscordAssistant.Services
{
    public class ReminderService : IReminderService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        //private readonly ReminderContext _reminderContext;
        private readonly DiscordShardedClient _shardedClient;
        private readonly ILogger<ReminderService> _logger;

        private int dispatchedRemindersCount = 0;
        private bool initialized = false;

        public ReminderService(IServiceScopeFactory serviceScopeFactory, /*ReminderContext reminderContext, */DiscordShardedClient shardedClient, ILogger<ReminderService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            //_reminderContext = reminderContext;
            _shardedClient = shardedClient;
            _logger = logger;
        }

        public void Start()
        {
            if (initialized)
            {
                return;
            }

            Task.Run(async () =>
            {
                Stopwatch stopwatch = new();
                stopwatch.Start();

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var reminderContext = scope.ServiceProvider.GetRequiredService<ReminderContext>();

                    foreach (var reminder in reminderContext.Reminders.AsNoTracking())
                    {
                        try
                        {
                            if (reminder.Cancelled is true)
                            {
                                reminderContext.Remove(reminder);

                                await reminderContext.SaveChangesAsync();

                                _logger.LogInformation("Removed reminder ID {Id} due to cancelled.", reminder.Id);
                            }

                            else
                            {
                                TimeSpan timeSpan = ClientUtilities.ConvertUnixTimestampToDateTime(reminder.UnixTimestampRemindAt) - DateTime.Now;

                                if (timeSpan.TotalSeconds < 0)
                                {
                                    await SendReminder(reminder);

                                    await RemoveReminder(reminder);

                                    _logger.LogInformation("Removed reminder ID {Id} due to late completion.", reminder.Id);
                                    dispatchedRemindersCount++;
                                }

                                else
                                {
                                    CreateReminderTask(reminder, timeSpan);
                                }
                            }
                        }

                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "An error occured while dispatching a reminder (ID: {Id}).", reminder.Id);
                        }
                    }
                }                

                stopwatch.Stop();

                _logger.LogInformation("Dispatched {DispatchedRemindersCount} reminders in {ElapsedMilliseconds} ms.", dispatchedRemindersCount, stopwatch.ElapsedMilliseconds);
            });

            initialized = true;

            _logger.LogInformation("Initialized reminder dispatch service.");
        }

        /// <summary>
        /// Composes a reminder message.
        /// </summary>
        public async Task<string> CreateReminderMessage(Reminder reminder)
        {
            var client = _shardedClient.GetShard(reminder.TargetGuildId);

            var targetChannel = await client.GetChannelAsync((ulong)reminder.TargetChannelId);

            var initiatingUser = await targetChannel.Guild.GetMemberAsync(reminder.InitiatingUserId);

            var remindAt = ClientUtilities.ConvertUnixTimestampToDateTime(reminder.UnixTimestampRemindAt);

            TimeSpan remainingTime = remindAt - DateTime.Now;

            int mentionCount = 0;

            foreach (char letter in reminder.TargetedUserOrRoleMention)
            {
                if (letter == '@')
                {
                    mentionCount++;
                }
            }

            // Late dispatch? Let them know how late has the reminder been.
            if (Math.Round(remainingTime.TotalMinutes) < -0)
            {
                if (mentionCount is 1 && reminder.TargetedUserOrRoleMention.Contains(reminder.InitiatingUserId.ToString()))
                {
                    return $"{DiscordEmoji.FromName(client, ":alarm_clock:")} {initiatingUser.Mention}, {Formatter.Timestamp(remindAt, TimestampFormat.RelativeTime)}, you wanted to be reminded of the following: \n\n{reminder.Content}";
                }

                else
                {
                    return $"{DiscordEmoji.FromName(client, ":alarm_clock:")} {reminder.TargetedUserOrRoleMention}, {Formatter.Timestamp(remindAt, TimestampFormat.RelativeTime)}, {initiatingUser.Mention} wanted to remind you of the following: \n\n{reminder.Content}";
                }
            }

            // Else...
            if (mentionCount is 1 && reminder.TargetedUserOrRoleMention.Contains(reminder.InitiatingUserId.ToString()))
            {
                return $"{DiscordEmoji.FromName(client, ":alarm_clock:")} {initiatingUser.Mention}, you wanted to be reminded of the following: \n\n{reminder.Content}";
            }

            else
            {
                return $"{DiscordEmoji.FromName(client, ":alarm_clock:")} {reminder.TargetedUserOrRoleMention}, {initiatingUser.Mention} wanted to remind you of the following: \n\n{reminder.Content}";
            }
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
        /// Stores the reminder into the database and dispatches the reminder. It will check whether the reminder was cancelled before the reminder content is to be sent.
        /// </summary>
        public async Task RegisterReminder(TimeSpan remainingTime, DiscordChannel targetChannel, string remindMessage, string remindTarget, string displayTarget, CommandContext commandContext = null, InteractionContext interactionContext = null)
        {
            Reminder reminder = null;

            if (commandContext != null)
            {
                reminder = new Reminder()
                {
                    InitiatingUserId = commandContext.Member.Id,
                    TargetedUserOrRoleMention = remindTarget,
                    UnixTimestampRemindAt = ClientUtilities.ConvertDateTimeToUnixTimestamp(DateTime.UtcNow.Add(remainingTime)),
                    TargetGuildId = targetChannel.Guild.Id,
                    ReplyMessageId = commandContext.Message.ReferencedMessage is null ? commandContext.Message.Id : commandContext.Message.ReferencedMessage.Id,
                    TargetChannelId = targetChannel.Id,
                    Cancelled = false,
                    Content = remindMessage
                };

                if (commandContext.Channel != targetChannel)
                {
                    reminder.ReplyMessageId = commandContext.Message.Id;
                }
            }

            else
            {
                reminder = new Reminder()
                {
                    InitiatingUserId = interactionContext.Member.Id,
                    TargetedUserOrRoleMention = remindTarget,
                    UnixTimestampRemindAt = ClientUtilities.ConvertDateTimeToUnixTimestamp(DateTime.UtcNow.Add(remainingTime)),
                    TargetGuildId = targetChannel.Guild.Id,
                    TargetChannelId = targetChannel.Id,
                    Cancelled = false,
                    Content = remindMessage
                };
            }

            var reminderContext = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ReminderContext>();

            reminderContext.Add(reminder);

            await reminderContext.SaveChangesAsync();

            CreateReminderTask(reminder, remainingTime);

            _ = commandContext is not null ? commandContext.Channel.SendMessageAsync(CreateReminderReceiptMessage(remainingTime, remindMessage, displayTarget)) : interactionContext.CreateResponseAsync(CreateReminderReceiptMessage(remainingTime, remindMessage, displayTarget));
        }

        /// <summary>
        /// Creates and fires a task which sends a reminder message after delaying from the specified timespan. It will check whether the reminder was cancelled before the reminder content is to be sent.
        /// </summary>
        private void CreateReminderTask(Reminder reminder, TimeSpan remainingTime)
        {
            var reminderTask = new Task(async () =>
            {
                try
                {
                    long fullDelays = remainingTime.Ticks / Constant.maxTimeSpanValue.Ticks;
                    for (int i = 0; i < fullDelays; i++)
                    {
                        await Task.Delay(Constant.maxTimeSpanValue);
                        remainingTime -= Constant.maxTimeSpanValue;
                    }

                    await Task.Delay(remainingTime);

                    var row = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ReminderContext>()
                            .Reminders.AsNoTracking().FirstOrDefault(x => x.Id == reminder.Id); ;

                    if (row is null)
                    {
                        _logger.LogWarning("Reminder ID {Id} does not exist in database. Aborted sending reminder content.", reminder.Id);

                        return;
                    }

                    else if (row.Cancelled is true)
                    {
                        await RemoveReminder(reminder);
                        _logger.LogInformation("Removed reminder ID {Id} due to cancelled.", reminder.Id);

                        return;
                    }

                    await SendReminder(row);

                    await RemoveReminder(row);
                    _logger.LogInformation("Removed reminder ID {Id} due to completion.", reminder.Id);
                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured while processing dispatched reminder ID {Id}.", reminder.Id);
                }
            });

            reminderTask.Start();
            dispatchedRemindersCount++;
        }

        private async Task<Task> SendReminder(Reminder reminder)
        {
            string reminderMessage = await CreateReminderMessage(reminder);

            var targetChannel = await _shardedClient.GetShard(reminder.TargetGuildId).GetChannelAsync((ulong)reminder.TargetChannelId);
             
            try
            {
                var originalMessage = await targetChannel.GetMessageAsync((ulong)reminder.ReplyMessageId);

                await originalMessage.RespondAsync(reminderMessage);
            }

            catch
            {
                await targetChannel.SendMessageAsync(reminderMessage);
            }

            return Task.CompletedTask;
        }

        private async Task<Task> RemoveReminder(Reminder reminder)
        {
            var reminderContext = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ReminderContext>();

            reminderContext.Remove(reminder);

            await reminderContext.SaveChangesAsync();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends the list of active reminders that belongs to the respective guild.
        /// </summary>
        /// <returns></returns>
        public async Task SendGuildReminders(CommandContext commandContext = null, InteractionContext interactionContext = null)
        {
            var guild = commandContext is not null ? commandContext.Guild : interactionContext.Guild;

            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = $"Listing All Reminders for {guild.Name}...",
                Description = commandContext is not null ? $"To view commands to manage or list upcoming reminders, use {Formatter.Bold("osis reminder")}." : $"To view commands to manage or list upcoming reminders, use {Formatter.Bold("/reminder")}.",
                Timestamp = DateTime.Now,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                },
                Color = DiscordColor.MidnightBlue
            };

            IQueryable<Reminder> reminders = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ReminderContext>().Reminders.AsNoTracking().Where(x => x.Cancelled == false).Where(x => x.TargetGuildId == guild.Id);

            foreach (var reminder in reminders)
            {
                var initiatingUser = await guild.GetMemberAsync(reminder.InitiatingUserId);

                embedBuilder.AddField($"(ID: {reminder.Id}) by {initiatingUser.Username}#{initiatingUser.Discriminator} ({initiatingUser.DisplayName})", $"When: {Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(reminder.UnixTimestampRemindAt), TimestampFormat.LongDateTime)} ({Formatter.Timestamp(ClientUtilities.ConvertUnixTimestampToDateTime(reminder.UnixTimestampRemindAt), TimestampFormat.RelativeTime)})\nWho: {reminder.TargetedUserOrRoleMention}\nContent: {reminder.Content}", true);
            }

            if (reminders.Count() is 0)
            {
                _ = commandContext is not null ? commandContext.Channel.SendMessageAsync($"{Formatter.Bold("[ERROR]")} There are no reminders to display for this guild.") : interactionContext.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{Formatter.Bold("[ERROR]")} There are no reminders to display for this guild."));
            }

            else
            {
                _ = commandContext is not null ? commandContext.Channel.SendMessageAsync(embedBuilder.Build()) : interactionContext.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embedBuilder.Build()));
            }
        }

        public async Task SendTimespanHelpEmbedAsync(CommandContext commandContext = null, InteractionContext interactionContext = null)
        {
            var embed = new DiscordEmbedBuilder()
            {
                Title = "Invalid timespan value given!",
                Description = $"At the moment, I can only accept three forms of timespan, which is:\n• Shortened dates, example: {Formatter.InlineCode("25/06/2022")} or {Formatter.InlineCode("25/JUNE/2022")},\n• Relative time, example: {Formatter.InlineCode("2h")} to remind you in two hours or {Formatter.InlineCode("12h30m")} in twelve hours and 30 minutes,\n• (ONLY WORKS FOR GMT+7 TIMEZONE) Or you can just point out the time you want the reminder to be sent if you are going to remind them within the next 24 hours, example: {Formatter.InlineCode("11:30")}. Follow the 24 hours format when using this option.",
                Timestamp = DateTime.Now,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "OSIS Discord Assistant"
                },
                Color = DiscordColor.MidnightBlue
            };

            _ = commandContext is not null ? commandContext.Channel.SendMessageAsync(embed) : interactionContext.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()));
        }
    }
}
