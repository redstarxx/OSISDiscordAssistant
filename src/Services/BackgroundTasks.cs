using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using OSISDiscordAssistant.Models;
using OSISDiscordAssistant.Constants;
using Microsoft.Extensions.Logging;
using Humanizer;

namespace OSISDiscordAssistant.Services
{
    public class BackgroundTasks
    {
        /// <summary>
        /// Creates an events reminder task which queries the events table on a minute-by-minute basis and sends a reminder message for the respective event if the event meets the requirement for it to be sent.
        /// </summary>
        public static void StartEventReminders()
        {
            if (SharedData.IsEventReminderInitialized)
            {
                // TODO: Ensure services always run
                Bot.Client.Logger.LogInformation(EventIds.Services, "Events reminder task is already fired. Skipping initialization.", DateTime.Now);

                return;
            }

            Task eventReminder = Task.Run(async () =>
            {
                DiscordChannel eventsChannel = await Bot.Client.GetShard(SharedData.MainGuildId).GetChannelAsync(SharedData.EventChannelId);

                DiscordChannel errorLogsChannel = await Bot.Client.GetShard(SharedData.MainGuildId).GetChannelAsync(SharedData.ErrorChannelId);

                DiscordMessageBuilder reminderMessageBuilder = new DiscordMessageBuilder
                {
                };

                Stopwatch stopwatch = new Stopwatch();

                try
                {
                    while (true)
                    {
                        var reminderEmbed = new DiscordEmbedBuilder
                        {
                            Timestamp = DateTime.Now,
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = "OSIS Discord Assistant"
                            },
                            Color = DiscordColor.MidnightBlue
                        };

                        int counter = 0;

                        stopwatch.Start();

                        using (var db = new EventContext())
                        {
                            Stopwatch processingStopWatch = new Stopwatch();

                            bool sentReminder = false;

                            foreach (var row in db.Events)
                            {
                                processingStopWatch.Start();

                                var cultureInfo = new CultureInfo(row.EventDateCultureInfo);

                                DateTime currentDateTime = DateTime.Now.Subtract(TimeSpan.FromHours(DateTime.Now.Hour) + TimeSpan.FromMinutes(DateTime.Now.Minute) + TimeSpan.FromSeconds(DateTime.Now.Second));

                                DateTime parsedEventDateTime = DateTime.Parse(row.EventDate, cultureInfo);

                                TimeSpan timeSpan = parsedEventDateTime - currentDateTime;

                                double remainingDays = Math.Round(timeSpan.TotalDays);

                                reminderEmbed.Title = $"Events Manager - Reminding {row.EventName}... (Event ID: {row.Id})";

                                reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                reminderMessageBuilder.WithContent("@everyone");

                                if (remainingDays == 7)
                                {
                                    if (row.PreviouslyReminded == false)
                                    {
                                        reminderEmbed.Description = $"Attention council members! Next week will be the day for {Formatter.Bold(row.EventName)}! Read below to find out more about this event.";
                                        reminderMessageBuilder.WithEmbed(reminderEmbed.Build());

                                        await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.PreviouslyReminded = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }

                                        sentReminder = true;
                                    }
                                }

                                else if (remainingDays < 7 && remainingDays > 1)
                                {
                                    if (row.PreviouslyReminded == false)
                                    {
                                        reminderEmbed.Description = $"Attention council members! In {remainingDays} day(s), it will be the day for {Formatter.Bold(row.EventName)}! Read below to find out more about this event.";
                                        reminderMessageBuilder.WithEmbed(reminderEmbed.Build());

                                        await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.PreviouslyReminded = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }

                                        sentReminder = true;
                                    }
                                }

                                else if (remainingDays == 1)
                                {
                                    if (row.PreviouslyReminded == false)
                                    {
                                        reminderEmbed.Description = $"Attention council members! Tomorrow will be the day for {Formatter.Bold(row.EventName)}! Read below to find out more about this event.";
                                        reminderMessageBuilder.WithEmbed(reminderEmbed.Build());

                                        await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.PreviouslyReminded = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }

                                        sentReminder = true;
                                    }
                                }

                                else if (remainingDays < 1 && remainingDays > 0)
                                {
                                    if (row.PreviouslyReminded == false)
                                    {
                                        reminderEmbed.Description = $"Attention council members! {Formatter.Bold(row.EventName)} will be in effect in {Math.Round(timeSpan.TotalHours)} hours! Read below to find out more about this event.";
                                        reminderMessageBuilder.WithEmbed(reminderEmbed.Build());

                                        await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.PreviouslyReminded = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }

                                        sentReminder = true;
                                    }
                                }

                                else if (remainingDays < 0)
                                {
                                    // If the bot did not have a chance to remind the event before the event date, mark it as done anyway.
                                    if (remainingDays < 0)
                                    {
                                        if (row.Expired is false)
                                        {
                                            using (var dbUpdate = new EventContext())
                                            {
                                                Events rowToUpdate = null;
                                                rowToUpdate = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                                if (rowToUpdate != null)
                                                {
                                                    rowToUpdate.PreviouslyReminded = true;
                                                    rowToUpdate.ProposalReminded = true;
                                                    rowToUpdate.Expired = true;
                                                }

                                                dbUpdate.SaveChanges();

                                                Bot.Client.Logger.LogInformation(EventIds.Services, $"Marked event '{row.EventName}' (ID: {row.Id}) as expired.", DateTime.Now);
                                            }
                                        }
                                    }
                                }

                                if (parsedEventDateTime.ToShortDateString() == currentDateTime.ToShortDateString())
                                {
                                    if (row.Expired == false)
                                    {
                                        reminderEmbed.Description = $"Attention council members! Today is the day for {Formatter.Bold(row.EventName)}! Read the description below to know more.";
                                        reminderMessageBuilder.WithEmbed(reminderEmbed.Build());

                                        await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.PreviouslyReminded = true;
                                                _d.Expired = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }

                                        sentReminder = true;
                                    }
                                }

                                processingStopWatch.Stop();

                                if (sentReminder)
                                {
                                    sentReminder = false;

                                    Bot.Client.Logger.LogInformation(EventIds.Services, $"Sent event reminder for '{row.EventName}' (ID: {row.Id}) in {processingStopWatch.ElapsedMilliseconds} ms.", DateTime.Now);
                                }

                                processingStopWatch.Reset();

                                reminderEmbed.ClearFields();
                            }

                            await db.DisposeAsync();
                        }

                        stopwatch.Stop();
                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        if (counter != 0)
                        {
                            Bot.Client.Logger.LogInformation(EventIds.Services, $"Completed events reminder task in {elapsedMilliseconds} ms. Reminded {counter} ({counter.ToWords()}) events.", DateTime.Now);
                        }

                        else
                        {
                            Bot.Client.Logger.LogInformation(EventIds.Services, $"Completed events reminder task in {elapsedMilliseconds} ms. No events to remind.", DateTime.Now);
                        }

                        stopwatch.Reset();
                        await Task.Delay(TimeSpan.FromMinutes(1).Subtract(TimeSpan.FromMilliseconds(elapsedMilliseconds)));
                    }
                }

                catch (Exception ex)
                {
                    var exception = ex;
                    while (exception is AggregateException)
                        exception = exception.InnerException;

                    Bot.Client.Logger.LogCritical(EventIds.Services, $"Events reminder task threw an exception: {exception.GetType()}: {exception.Message}.", DateTime.Now);

                    await errorLogsChannel.SendMessageAsync($"{ex.Message}");
                }
            });

            SharedData.IsEventReminderInitialized = true;

            Bot.Client.Logger.LogInformation(EventIds.Services, "Initialized events reminder task.", DateTime.Now);
        }

        /// <summary>
        /// Creates a proposal submission reminder task which queries the events table on a minute-by-minute basis and sends a reminder message for the respective event if the event meets the requirement for it to be sent.
        /// </summary>
        public static void StartProposalReminders()
        {
            if (SharedData.IsProposalReminderInitialized)
            {
                Bot.Client.Logger.LogInformation(EventIds.Services, "Proposal reminder task is already fired. Skipping initialization.", DateTime.Now);

                return;
            }

            Task proposalReminder = Task.Run(async () =>
            {
                DiscordChannel proposalChannel = await Bot.Client.GetShard(SharedData.MainGuildId).GetChannelAsync(SharedData.ProposalChannelId);

                DiscordChannel errorLogsChannel = await Bot.Client.GetShard(SharedData.MainGuildId).GetChannelAsync(SharedData.ErrorChannelId);

                DiscordMessageBuilder reminderMessageBuilder = new DiscordMessageBuilder
                {
                };

                Stopwatch stopwatch = new Stopwatch();

                try
                {
                    while (true)
                    {

                        var reminderEmbed = new DiscordEmbedBuilder
                        {
                            Timestamp = DateTime.Now,
                            Footer = new DiscordEmbedBuilder.EmbedFooter
                            {
                                Text = "OSIS Discord Assistant"
                            },
                            Color = DiscordColor.MidnightBlue
                        };

                        int counter = 0;

                        stopwatch.Start();
                        
                        using (var db = new EventContext())
                        {
                            Stopwatch processingStopWatch = new Stopwatch();

                            bool sentReminder = false;

                            foreach (var row in db.Events)
                            {
                                processingStopWatch.Start();

                                var cultureInfo = new CultureInfo(row.EventDateCultureInfo);

                                DateTime currentDateTime = DateTime.Now.Subtract(TimeSpan.FromHours(DateTime.Now.Hour) + TimeSpan.FromMinutes(DateTime.Now.Minute) + TimeSpan.FromSeconds(DateTime.Now.Second));

                                DateTime parsedEventDateTime = DateTime.Parse(row.EventDate, cultureInfo);

                                TimeSpan timeSpan = parsedEventDateTime - currentDateTime;

                                double remainingDays = Math.Round(timeSpan.TotalDays);

                                if (remainingDays == 30 || remainingDays > 6 && remainingDays < 30)
                                {
                                    if (row.ProposalReminded == false)
                                    {
                                        reminderEmbed.Title = $"Events Manager - Proposal Submission for {row.EventName}... (Event ID: {row.Id})";
                                        reminderEmbed.Description = $"Make sure you have submitted your respective proposals in preparation for {Formatter.Bold(row.EventName)}!";

                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent(row.PersonInCharge)
                                                              .WithEmbed(embed: reminderEmbed);

                                        await proposalChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.ProposalReminded = true;
                                            }

                                            dbUpdate.SaveChanges();
                                        }

                                        sentReminder = true;
                                    }
                                }

                                if (parsedEventDateTime.ToShortDateString() == currentDateTime.ToShortDateString())
                                {
                                    if (row.ProposalReminded == false)
                                    {
                                        using (var dbUpdate = new EventContext())
                                        {
                                            Events _d = null;
                                            _d = dbUpdate.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (_d != null)
                                            {
                                                _d.ProposalReminded = true;
                                            }

                                            dbUpdate.SaveChanges();

                                            Bot.Client.Logger.LogInformation(EventIds.Services, $"Marked '{row.EventName}' (ID: {row.Id}) proposal reminded column as {row.ProposalReminded}.", DateTime.Now);
                                        }
                                    }
                                }

                                processingStopWatch.Stop();

                                if (sentReminder)
                                {
                                    sentReminder = false;

                                    Bot.Client.Logger.LogInformation(EventIds.Services, $"Sent proposal reminder for '{row.EventName}' (ID: {row.Id}) in {processingStopWatch.ElapsedMilliseconds} ms.", DateTime.Now);
                                }

                                processingStopWatch.Reset();
                            }

                            await db.DisposeAsync();
                        }

                        stopwatch.Stop();
                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        if (counter != 0)
                        {
                            Bot.Client.Logger.LogInformation(EventIds.Services, $"Completed proposal reminder task in {elapsedMilliseconds} ms. Reminded {counter} ({counter.ToWords()}) proposal submissions.", DateTime.Now);
                        }

                        else
                        {
                            Bot.Client.Logger.LogInformation(EventIds.Services, $"Completed proposal reminder task in {elapsedMilliseconds} ms. No proposal submissions to remind.", DateTime.Now);
                        }

                        stopwatch.Reset();
                        await Task.Delay(TimeSpan.FromMinutes(1).Subtract(TimeSpan.FromMilliseconds(elapsedMilliseconds)));
                    }
                }

                catch (Exception ex)
                {
                    var exception = ex;
                    while (exception is AggregateException)
                        exception = exception.InnerException;

                    Bot.Client.Logger.LogCritical(EventIds.Services, $"Proposal submission reminder task threw an exception: {exception.GetType()}: {exception.Message}.", DateTime.Now);

                    await errorLogsChannel.SendMessageAsync($"{ex.Message}");
                }
            });

            SharedData.IsProposalReminderInitialized = true;

            Bot.Client.Logger.LogInformation(EventIds.Services, "Initialized proposal submission reminder task.", DateTime.Now);
        }

        /// <summary>
        /// Creates a status updater task that changes the bot's display status every two minutes.
        /// </summary>
        public static void StartStatusUpdater()
        {
            if (SharedData.IsStatusUpdaterInitialized)
            {
                Bot.Client.Logger.LogInformation(EventIds.Services, "Status updater task is already fired. Skipping initialization.", DateTime.Now);

                return;
            }

            Task statusUpdater = Task.Run(async () =>
            {
                Stopwatch stopwatch = new Stopwatch();

                int index = 0;

                string currentCustomStatus = null;

                List<string> customStatusList = new List<string>();

                foreach (string customStatus in SharedData.CustomStatusDiplay)
                {
                    customStatusList.Add(customStatus);
                }

                ActivityType activityType = new ActivityType();

                switch (SharedData.StatusActivityType)
                {
                    case 0:
                        activityType = ActivityType.Playing;
                        break;
                    case 1:
                        activityType = ActivityType.Streaming;
                        break;
                    case 2:
                        activityType = ActivityType.ListeningTo;
                        break;
                    case 3:
                        activityType = ActivityType.Watching;
                        break;
                    case 4:
                        activityType = ActivityType.Custom;
                        break;
                    case 5:
                        activityType = ActivityType.Competing;
                        break;
                    default:
                        break;
                }

                while (true)
                {
                    stopwatch.Start();

                    try
                    {
                        currentCustomStatus = customStatusList[index];

                        index++;
                    }

                    // Index out of range? Handle it down here.
                    catch
                    {
                        index = 0;
                        currentCustomStatus = customStatusList[index];

                        index++;
                    }

                    var activity = new DiscordActivity(currentCustomStatus, activityType);
                    await Bot.Client.UpdateStatusAsync(activity);                 

                    stopwatch.Stop();
                    long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    stopwatch.Reset();

                    Bot.Client.Logger.LogInformation(EventIds.StatusUpdater, $"Presence updated: '{activity.ActivityType} {activity.Name}' in {elapsedMilliseconds} ms.", DateTime.Now);

                    await Task.Delay(TimeSpan.FromMinutes(2).Subtract(TimeSpan.FromMilliseconds(elapsedMilliseconds)));
                }
            });

            SharedData.IsStatusUpdaterInitialized = true;

            Bot.Client.Logger.LogInformation(EventIds.StatusUpdater, "Initialized status updater task.", DateTime.Now);
        }

        public static void StartVerificationCleanupTask()
        {
            if (SharedData.IsVerificationCleanupTaskInitialized)
            {
                Bot.Client.Logger.LogInformation(EventIds.Services, "Verification cleanup task is already fired. Skipping initialization.", DateTime.Now);

                return;
            }

            Task verificationCleaningServiceTask = Task.Run(async () =>
            {
                DiscordChannel verificationProcessingChannel = await Bot.Client.GetShard(SharedData.MainGuildId).GetChannelAsync(SharedData.VerificationRequestsProcessingChannelId);

                Stopwatch stopwatch = new Stopwatch();

                int counter = 0;

                try
                {
                    while (true)
                    {
                        stopwatch.Start();

                        using (var db = new VerificationContext())
                        {
                            foreach (var row in db.Verifications)
                            {
                                var requestEmbed = await verificationProcessingChannel.GetMessageAsync(row.VerificationEmbedId);

                                foreach (var embed in requestEmbed.Embeds.ToList())
                                {
                                    DateTimeOffset offset = (DateTimeOffset)embed.Timestamp;
                                    DateTime embedTimestamp = offset.DateTime;

                                    if (embedTimestamp.Subtract(TimeSpan.FromSeconds(DateTime.Now.Second)).AddDays(SharedData.MaxPendingVerificationWaitingDay) == DateTime.Now.Subtract(TimeSpan.FromSeconds(DateTime.Now.Second)) ||
                                    DateTime.Now.Subtract(TimeSpan.FromSeconds(DateTime.Now.Second)) > embedTimestamp.Subtract(TimeSpan.FromSeconds(DateTime.Now.Second)).AddDays(SharedData.MaxPendingVerificationWaitingDay))
                                    {
                                        counter++;

                                        DiscordEmbed updatedEmbed = null;

                                        DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder(embed)
                                        {
                                            Title = $"{embed.Title.Replace(" | PENDING", " | EXPIRED")}",
                                            Description = $"{embed.Description.Replace("PENDING.", $"EXPIRED (nobody handled this request, at {Formatter.Timestamp(DateTime.Now, TimestampFormat.LongDateTime)}).")}"
                                        };

                                        updatedEmbed = embedBuilder.Build();

                                        await requestEmbed.ModifyAsync(x => x.WithEmbed(updatedEmbed));

                                        await Bot.Client.GetShard(SharedData.MainGuildId).GetGuildAsync(SharedData.MainGuildId).Result
                                        .GetMemberAsync(row.UserId).Result.SendMessageAsync($"{Formatter.Bold("[VERIFICATION]")} I'm sorry, your verification request has expired (nobody responded to it within {SharedData.MaxPendingVerificationWaitingDay} ({SharedData.MaxPendingVerificationWaitingDay.ToWords()}) days). Feel free to try again or reach out to a member of Inti OSIS for assistance.");

                                        var request = db.Verifications.SingleOrDefault(x => x.VerificationEmbedId == row.VerificationEmbedId);

                                        db.Remove(request);
                                        await db.SaveChangesAsync();

                                        Bot.Client.Logger.LogInformation(EventIds.Services, $"Removed verification request message ID {requestEmbed.Id}.", DateTime.Now);
                                    }
                                }
                            }
                        }

                        stopwatch.Stop();

                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        Bot.Client.Logger.LogInformation(EventIds.Services, $"Completed verification request cleanup task in {elapsedMilliseconds} ms. Removed {counter} ({counter.ToWords()}) requests.", DateTime.Now);

                        counter = 0;
                        stopwatch.Reset();
                        await Task.Delay(TimeSpan.FromMinutes(1).Subtract(TimeSpan.FromMilliseconds(elapsedMilliseconds)));
                    }
                }

                catch (Exception ex)
                {
                    var exception = ex;
                    while (exception is AggregateException)
                        exception = exception.InnerException;

                    Bot.Client.Logger.LogCritical(EventIds.Services, $"Verification cleanup task threw an exception: {exception.GetType()}: {exception.Message}.", DateTime.Now);
                }
            });

            SharedData.IsVerificationCleanupTaskInitialized = true;

            Bot.Client.Logger.LogInformation(EventIds.Services, "Initialized verification cleanup task.", DateTime.Now);
        }

        /// <summary>
        /// Fires a task that prevents zombied connection from persisting more than 5 minutes. Docker will handle the re-initialization of the bot if it is terminated.
        /// </summary>
        public static void StartHeartbeatMonitoringTask()
        {
            if (SharedData.IsHeartbeatMonitoringTaskInitialized)
            {
                Bot.Client.Logger.LogInformation(EventIds.Services, "Heartbeat monitoring task is already fired. Skipping initialization.", DateTime.Now);

                return;
            }

            Task verificationCleaningServiceTask = Task.Run(async () =>
            {
                while (true)
                {
                    DateTime lastMonitored = DateTime.Now;

                    await Task.Delay(TimeSpan.FromMinutes(5));

                    if (SharedData.ReceivedHeartbeats is 0)
                    {
                        Bot.Client.Logger.LogCritical(EventIds.Core, $"No heartbeat has been received since {lastMonitored}. Terminating...", DateTime.Now);

                        await Bot.Client.StopAsync();

                        Environment.Exit(0);
                    }

                    else
                    {
                        Bot.Client.Logger.LogInformation(EventIds.Core, $"Received {SharedData.ReceivedHeartbeats} heartbeats since {lastMonitored}. Resetting received heartbeats counter to 0.", DateTime.Now);

                        SharedData.ReceivedHeartbeats = 0;
                    }
                }
            });

            SharedData.IsHeartbeatMonitoringTaskInitialized = true;

            Bot.Client.Logger.LogInformation(EventIds.Services, "Initialized heartbeat monitoring task.", DateTime.Now);
        }
    }
}
