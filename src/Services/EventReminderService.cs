using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Logging;
using OSISDiscordAssistant.Constants;
using OSISDiscordAssistant.Models;

namespace OSISDiscordAssistant.Services
{
    public class EventReminderService : IEventReminderService
    {
        private readonly ILogger<EventReminderService> _logger;
        private readonly EventContext _eventContext;
        private readonly DiscordShardedClient _shardedClient;

        public EventReminderService(ILogger<EventReminderService> logger, EventContext eventContext, DiscordShardedClient shardedClient)
        {
            _logger = logger;
            _eventContext = eventContext;
            _shardedClient = shardedClient;
        }

        public void Start()
        {
            if (SharedData.IsEventReminderInitialized)
            {
                // TODO: Ensure services always run
                _logger.LogInformation("Events reminder task is already fired. Skipping initialization.", DateTime.Now);

                return;
            }

            Task eventReminder = Task.Run(async () =>
            {
                DiscordChannel eventsChannel = await _shardedClient.GetShard(SharedData.MainGuildId).GetChannelAsync(SharedData.EventChannelId);

                DiscordChannel errorLogsChannel = await _shardedClient.GetShard(SharedData.MainGuildId).GetChannelAsync(SharedData.ErrorChannelId);

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

                        Stopwatch processingStopWatch = new Stopwatch();

                        bool sentReminder = false;

                        foreach (var row in _eventContext.Events)
                        {
                            processingStopWatch.Start();

                            var cultureInfo = new CultureInfo(row.EventDateCultureInfo);

                            DateTime currentDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

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

                                    Events _d = null;
                                    _d = _eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                    if (_d != null)
                                    {
                                        _d.PreviouslyReminded = true;
                                    }

                                    _eventContext.SaveChanges();

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

                                    Events _d = null;
                                    _d = _eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                    if (_d != null)
                                    {
                                        _d.PreviouslyReminded = true;
                                    }

                                    _eventContext.SaveChanges();

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

                                    Events _d = null;
                                    _d = _eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                    if (_d != null)
                                    {
                                        _d.PreviouslyReminded = true;
                                    }

                                    _eventContext.SaveChanges();

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

                                    Events _d = null;
                                    _d = _eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                    if (_d != null)
                                    {
                                        _d.PreviouslyReminded = true;
                                    }

                                    _eventContext.SaveChanges();

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
                                        Events rowToUpdate = null;
                                        rowToUpdate = _eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                        if (rowToUpdate != null)
                                        {
                                            rowToUpdate.PreviouslyReminded = true;
                                            rowToUpdate.ProposalReminded = true;
                                            rowToUpdate.Expired = true;
                                        }

                                        _eventContext.SaveChanges();

                                        _logger.LogInformation($"Marked event '{row.EventName}' (ID: {row.Id}) as expired.", DateTime.Now);
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

                                    Events _d = null;
                                    _d = _eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                    if (_d != null)
                                    {
                                        _d.PreviouslyReminded = true;
                                        _d.Expired = true;
                                    }

                                    _eventContext.SaveChanges();

                                    sentReminder = true;
                                }
                            }

                            processingStopWatch.Stop();

                            if (sentReminder)
                            {
                                sentReminder = false;

                                _logger.LogInformation($"Sent event reminder for '{row.EventName}' (ID: {row.Id}) in {processingStopWatch.ElapsedMilliseconds} ms.", DateTime.Now);
                            }

                            processingStopWatch.Reset();

                            reminderEmbed.ClearFields();
                        }

                        stopwatch.Stop();
                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        if (counter != 0)
                        {
                            _logger.LogInformation($"Completed events reminder task in {elapsedMilliseconds} ms. Reminded {counter} ({counter.ToWords()}) events.", DateTime.Now);
                        }

                        else
                        {
                            _logger.LogInformation($"Completed events reminder task in {elapsedMilliseconds} ms. No events to remind.", DateTime.Now);
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

                    _logger.LogCritical($"Events reminder task threw an exception: {exception.GetType()}: {exception.Message}.", DateTime.Now);

                    await errorLogsChannel.SendMessageAsync($"{ex.Message}");
                }
            });

            SharedData.IsEventReminderInitialized = true;

            _logger.LogInformation("Initialized events reminder task.", DateTime.Now);
        }
    }
}
