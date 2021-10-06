using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using OSISDiscordAssistant.Utilities;
using OSISDiscordAssistant.Constants;
using OSISDiscordAssistant.Models;
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
                return;
            }

            Task eventReminder = Task.Run(async () =>
            {
                DiscordChannel eventsChannel = await Bot.Client.GetShard(StringConstants.MainGuildId).GetChannelAsync(StringConstants.EventChannel);

                DiscordChannel errorLogsChannel = await Bot.Client.GetShard(StringConstants.MainGuildId).GetChannelAsync(StringConstants.ErrorChannel);

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
                            Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
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

                            foreach (var row in db.Events)
                            {
                                processingStopWatch.Start();

                                bool sentReminder = false;

                                var cultureInfo = new CultureInfo(row.EventDateCultureInfo);

                                // Add 7 hours ahead because for some reason Linux doesn't pick the user preferred timezone.
                                DateTime currentDateTime = ClientUtilities.GetWesternIndonesianDateTime();

                                DateTime parseEventDateTime = DateTime.Parse(row.EventDate, cultureInfo);

                                TimeSpan timeSpan = parseEventDateTime - currentDateTime;

                                if (timeSpan.Days == 7)
                                {
                                    if (row.PreviouslyReminded == false)
                                    {
                                        reminderEmbed.Title = $"Events Manager - Reminding {row.EventName}... (Event ID: {row.Id})";
                                        reminderEmbed.Description = $"Attention council members! Next week will be the day for {Formatter.Bold(row.EventName)}! Read below to find out more about this event.";

                                        reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent("@everyone")
                                                              .WithEmbed(embed: reminderEmbed);

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

                                else if (timeSpan.Days < 7 && timeSpan.Days > 1)
                                {
                                    if (row.PreviouslyReminded == false)
                                    {
                                        reminderEmbed.Title = $"Events Manager - Reminding {row.EventName}... (Event ID: {row.Id})";
                                        reminderEmbed.Description = $"Attention council members! In {timeSpan.Days} day(s), it will be the day for {Formatter.Bold(row.EventName)}! Read below to find out more about this event.";

                                        reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent("@everyone")
                                                              .WithEmbed(embed: reminderEmbed);

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

                                else if (timeSpan.Days == 1)
                                {
                                    if (row.PreviouslyReminded == false)
                                    {
                                        reminderEmbed.Title = $"Events Manager - Reminding {row.EventName}... (Event ID: {row.Id})";
                                        reminderEmbed.Description = $"Attention council members! Tomorrow will be the day for {Formatter.Bold(row.EventName)}! Read below to find out more about this event.";

                                        reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent("@everyone")
                                                              .WithEmbed(embed: reminderEmbed);

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

                                else if (timeSpan.Days < 1)
                                {
                                    if (row.PreviouslyReminded == false)
                                    {
                                        reminderEmbed.Title = $"Events Manager - Reminding {row.EventName}... (Event ID: {row.Id})";
                                        reminderEmbed.Description = $"Attention council members! {Formatter.Bold(row.EventName)} will be in effect in {timeSpan.Hours} hours! Read below to find out more about this event.";

                                        reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent("@everyone")
                                                              .WithEmbed(embed: reminderEmbed);

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
                                    }

                                    sentReminder = true;
                                }

                                if (parseEventDateTime.ToShortDateString() == currentDateTime.ToShortDateString())
                                {
                                    if (row.Expired == false)
                                    {
                                        reminderEmbed.Title = $"Events Manager - Reminding {row.EventName}... (Event ID: {row.Id})";
                                        reminderEmbed.Description = $"Attention council members! Today is the day for {Formatter.Bold(row.EventName)}! Read the description below to know more.";

                                        reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", row.EventDate, true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent("@everyone")
                                                              .WithEmbed(embed: reminderEmbed);

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
                                    }

                                    sentReminder = true;
                                }

                                processingStopWatch.Stop();

                                if (sentReminder)
                                {
                                    sentReminder = false;

                                    Bot.Client.Logger.LogInformation($"Sent event reminder for '{row.EventName}' (ID: {row.Id}) in {processingStopWatch.ElapsedMilliseconds} ms. Date: {row.EventDate} (culture-info: {row.EventDateCultureInfo}), person-in-charge: {row.PersonInCharge}, proposal_reminded: {row.ProposalReminded}, previously_reminded: {row.PreviouslyReminded}, expired: {row.Expired}");
                                }

                                processingStopWatch.Reset();
                            }

                            await db.DisposeAsync();
                        }

                        stopwatch.Stop();
                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        if (counter != 0)
                        {
                            Bot.Client.Logger.LogInformation(Bot.ERTask, $"Completed events reminder task in {elapsedMilliseconds} ms. Reminded {counter} ({counter.ToWords()}) events.", ClientUtilities.GetWesternIndonesianDateTime());
                        }

                        else
                        {
                            Bot.Client.Logger.LogInformation(Bot.ERTask, $"Completed events reminder task in {elapsedMilliseconds} ms. No events to remind.", ClientUtilities.GetWesternIndonesianDateTime());
                        }

                        stopwatch.Reset();
                        await Task.Delay(TimeSpan.FromMinutes(1));
                    }
                }

                catch (Exception ex)
                {
                    var exception = ex;
                    while (exception is AggregateException)
                        exception = exception.InnerException;

                    Bot.Client.Logger.LogCritical(Bot.ERTask, $"Events reminder task threw an exception: {exception.GetType()}: {exception.Message}", DateTime.Now);

                    await errorLogsChannel.SendMessageAsync($"{ex.Message}").ConfigureAwait(false);
                }
            });

            SharedData.IsEventReminderInitialized = true;

            Bot.Client.Logger.LogInformation(Bot.ERTask, "Initialized events reminder task.", ClientUtilities.GetWesternIndonesianDateTime());
        }

        /// <summary>
        /// Creates a proposal submission reminder task which queries the events table on a minute-by-minute basis and sends a reminder message for the respective event if the event meets the requirement for it to be sent.
        /// </summary>
        public static void StartProposalReminders()
        {
            if (SharedData.IsProposalReminderInitialized)
            {
                return;
            }

            Task proposalReminder = Task.Run(async () =>
            {
                DiscordChannel proposalChannel = await Bot.Client.GetShard(StringConstants.MainGuildId).GetChannelAsync(StringConstants.ProposalChannel);

                DiscordChannel errorLogsChannel = await Bot.Client.GetShard(StringConstants.MainGuildId).GetChannelAsync(StringConstants.ErrorChannel);

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
                            Timestamp = ClientUtilities.GetWesternIndonesianDateTime(),
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

                            foreach (var row in db.Events)
                            {
                                processingStopWatch.Start();

                                bool sentReminder = false;

                                var cultureInfo = new CultureInfo(row.EventDateCultureInfo);

                                // Add 7 hours ahead because for some reason Linux doesn't pick the user preferred timezone.
                                DateTime currentDateTime = ClientUtilities.GetWesternIndonesianDateTime();

                                DateTime parseEventDateTime = DateTime.Parse(row.EventDate, cultureInfo);

                                TimeSpan timeSpan = parseEventDateTime - currentDateTime;

                                if (timeSpan.Days == 30 || timeSpan.Days > 6 && timeSpan.Days < 30)
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

                                if (parseEventDateTime.ToShortDateString() == currentDateTime.ToShortDateString())
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
                                        }

                                        Bot.Client.Logger.LogInformation($"Marked '{row.EventName}' (ID: {row.Id}) proposal_reminded as {row.ProposalReminded}.");
                                    }
                                }

                                processingStopWatch.Stop();

                                if (sentReminder)
                                {
                                    sentReminder = false;

                                    Bot.Client.Logger.LogInformation($"Sent proposal reminder for '{row.EventName}' (ID: {row.Id}) in {processingStopWatch.ElapsedMilliseconds} ms. Date: {row.EventDate} (culture-info: {row.EventDateCultureInfo}), person-in-charge: {row.PersonInCharge}, proposal_reminded: {row.ProposalReminded}, previously_reminded: {row.PreviouslyReminded}, expired: {row.Expired}");
                                }

                                processingStopWatch.Reset();
                            }

                            await db.DisposeAsync();
                        }

                        stopwatch.Stop();
                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        if (counter != 0)
                        {
                            Bot.Client.Logger.LogInformation(Bot.PRTask, $"Completed proposal reminder task in {elapsedMilliseconds} ms. Reminded {counter} ({counter.ToWords()}) proposal submissions.", ClientUtilities.GetWesternIndonesianDateTime());
                        }

                        else
                        {
                            Bot.Client.Logger.LogInformation(Bot.PRTask, $"Completed proposal reminder task in {elapsedMilliseconds} ms. No proposal submissions to remind.", ClientUtilities.GetWesternIndonesianDateTime());
                        }

                        stopwatch.Reset();
                        await Task.Delay(TimeSpan.FromMinutes(1));
                    }
                }

                catch (Exception ex)
                {
                    var exception = ex;
                    while (exception is AggregateException)
                        exception = exception.InnerException;

                    Bot.Client.Logger.LogCritical(Bot.PRTask, $"Proposal reminder task threw an exception: {exception.GetType()}: {exception.Message}", DateTime.Now);

                    await errorLogsChannel.SendMessageAsync($"{ex.Message}").ConfigureAwait(false);
                }
            });

            SharedData.IsProposalReminderInitialized = true;

            Bot.Client.Logger.LogInformation(Bot.PRTask, "Initialized proposal reminder task.", ClientUtilities.GetWesternIndonesianDateTime());
        }

        /// <summary>
        /// Creates a status updater task that changes the bot's display status every two minutes.
        /// </summary>
        public static void StartStatusUpdater()
        {
            if (SharedData.IsStatusUpdaterInitialized)
            {
                return;
            }

            Task statusUpdater = Task.Run(async () =>
            {
                string gradeNumber = "VII";

                while (true)
                {
                    var activity = new DiscordActivity("Grade " + gradeNumber, ActivityType.Watching);
                    await Bot.Client.UpdateStatusAsync(activity);

                    switch (gradeNumber)
                    {
                        case "VII":
                            gradeNumber = "VIII";
                            break;
                        case "VIII":
                            gradeNumber = "IX";
                            break;
                        case "IX":
                            gradeNumber = "X SCIENCE";
                            break;
                        case "X SCIENCE":
                            gradeNumber = "X SOCIAL";
                            break;
                        case "X SOCIAL":
                            gradeNumber = "XI SCIENCE";
                            break;
                        case "XI SCIENCE":
                            gradeNumber = "XI SOCIAL";
                            break;
                        case "XI SOCIAL":
                            gradeNumber = "XII SCIENCE";
                            break;
                        case "XII SCIENCE":
                            gradeNumber = "XII SOCIAL";
                            break;
                        case "XII SOCIAL":
                            gradeNumber = "VII";
                            break;
                        default:
                            break;
                    }

                    Bot.Client.Logger.LogInformation(Bot.StatusUpdater, $"Presence updated: {activity.ActivityType} {activity.Name}", DateTime.Now);

                    await Task.Delay(TimeSpan.FromMinutes(2));
                }
            });

            SharedData.IsStatusUpdaterInitialized = true;

            Bot.Client.Logger.LogInformation(Bot.StatusUpdater, "Initialized status updater task.", ClientUtilities.GetWesternIndonesianDateTime());
        }
    }
}
