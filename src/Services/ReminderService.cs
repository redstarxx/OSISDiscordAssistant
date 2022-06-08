using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OSISDiscordAssistant.Constants;
using OSISDiscordAssistant.Models;
using OSISDiscordAssistant.Utilities;

namespace OSISDiscordAssistant.Services
{
    public class ReminderService : IReminderService
    {
        private readonly ReminderContext _reminderContext;
        private readonly DiscordShardedClient _shardedClient;
        private readonly ILogger<ReminderService> _logger;

        private int dispatchedRemindersCount = 0;
        private bool initialized = false;

        public ReminderService(ReminderContext reminderContext, DiscordShardedClient shardedClient, ILogger<ReminderService> logger)
        {
            _reminderContext = reminderContext;
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

                foreach (var reminder in _reminderContext.Reminders.AsNoTracking())
                {
                    try
                    {
                        if (reminder.Cancelled is true)
                        {
                            _reminderContext.Remove(reminder);

                            await _reminderContext.SaveChangesAsync();

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

            // Late dispatch? Let them know how late has the reminder been.
            if (Math.Round(remainingTime.TotalMinutes) < -0)
            {
                if (initiatingUser.Mention == reminder.TargetedUserOrRoleMention)
                {
                    return $"{DiscordEmoji.FromName(client, ":alarm_clock:")} {initiatingUser.Mention}, {Formatter.Timestamp(remindAt, TimestampFormat.RelativeTime)}, you wanted to be reminded of the following: \n\n{reminder.Content}";
                }

                else
                {
                    return $"{DiscordEmoji.FromName(client, ":alarm_clock:")} {reminder.TargetedUserOrRoleMention}, {Formatter.Timestamp(remindAt, TimestampFormat.RelativeTime)}, {initiatingUser.Mention} wanted to remind you of the following: \n\n{reminder.Content}";
                }
            }

            // Else...
            if (initiatingUser.Mention == reminder.TargetedUserOrRoleMention)
            {
                return $"{DiscordEmoji.FromName(client, ":alarm_clock:")} {initiatingUser.Mention}, you wanted to be reminded of the following: \n\n{reminder.Content}";
            }

            else
            {
                return $"{DiscordEmoji.FromName(client, ":alarm_clock:")} {reminder.TargetedUserOrRoleMention}, {initiatingUser.Mention} wanted to remind you of the following: \n\n{reminder.Content}";
            }
        }

        /// <summary>
        /// Creates and fires a task which sends a reminder message after delaying from the specified timespan. It will check whether the reminder was cancelled before the reminder content is to be sent.
        /// </summary>
        public void CreateReminderTask(Reminder reminder, TimeSpan remainingTime)
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

                    var row = _reminderContext.Reminders.AsNoTracking().FirstOrDefault(x => x.Id == reminder.Id);

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
            _reminderContext.Remove(reminder);

            await _reminderContext.SaveChangesAsync();

            return Task.CompletedTask;
        }
    }
}
