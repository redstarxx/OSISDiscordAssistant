using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OSISDiscordAssistant.Models;
using OSISDiscordAssistant.Utilities;

namespace OSISDiscordAssistant.Services
{
    public class EventReminderService : IEventReminderService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EventReminderService> _logger;
        private readonly DiscordShardedClient _shardedClient;

        private bool initialized = false;

        public EventReminderService(IServiceScopeFactory serviceScopeFactory, ILogger<EventReminderService> logger, DiscordShardedClient shardedClient)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _shardedClient = shardedClient;
        }

        public void Start()
        {
            if (initialized)
            {
                return;
            }

            Task.Run(async () =>
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

                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var eventContext = scope.ServiceProvider.GetRequiredService<EventContext>();

                            foreach (var row in eventContext.Events.Where(x => x.Expired == false && x.ReminderDisabled == false))
                            {
                                try
                                {
                                    processingStopWatch.Start();

                                    DateTime eventDateTime = ClientUtilities.ConvertUnixTimestampToDateTime(row.EventDateUnixTimestamp);

                                    DateTime nextEventReminderDateTime = ClientUtilities.ConvertUnixTimestampToDateTime(row.NextScheduledReminderUnixTimestamp);

                                    TimeSpan timeSpan = eventDateTime - DateTime.Today;

                                    reminderEmbed.Title = $"ARTEMIS - Reminding {row.EventName}... (ID: {row.Id})";

                                    reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                    reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", Formatter.Timestamp(eventDateTime, TimestampFormat.LongDate), true);
                                    reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, false);

                                    reminderMessageBuilder.WithContent("@everyone");

                                    if (DateTime.Today == nextEventReminderDateTime || (DateTime.Today > nextEventReminderDateTime && DateTime.Today < eventDateTime))
                                    {
                                        if (row.ExecutedReminderLevel is 1 or 2 or 3)
                                        {
                                            reminderEmbed.Description = timeSpan.Days is not 1 ? $"In {timeSpan.Days} days, it will be the day for {Formatter.Bold(row.EventName)}!" : $"Tomorrow will be the day for {Formatter.Bold(row.EventName)}!";

                                            if (row.ProposalFileContent is not null)
                                            {
                                                string fileTitle = null;

                                                byte[] fileContent = null;

                                                MemoryStream fileStream = new MemoryStream();

                                                fileTitle = row.ProposalFileTitle;

                                                fileContent = row.ProposalFileContent;

                                                fileStream = new MemoryStream(fileContent);

                                                reminderMessageBuilder.WithFiles(new Dictionary<string, Stream>() { { fileTitle, fileStream } }, true);
                                            }

                                            reminderMessageBuilder.WithEmbed(reminderEmbed.Build());

                                            await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                            counter++;

                                            Events eventData = eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (eventData != null)
                                            {
                                                var calculatedNextReminderData = ClientUtilities.CalculateEventReminderDate(row.EventDateUnixTimestamp, true);

                                                eventData.NextScheduledReminderUnixTimestamp = calculatedNextReminderData.NextScheduledReminderUnixTimestamp;
                                                eventData.ExecutedReminderLevel = calculatedNextReminderData.ExecutedReminderLevel;
                                                eventData.Expired = calculatedNextReminderData.Expired;

                                                await eventContext.SaveChangesAsync();
                                            }

                                            sentReminder = true;
                                        }

                                        else if (row.ExecutedReminderLevel is 4)
                                        {
                                            reminderEmbed.Description = $"Tomorrow will be the day for {Formatter.Bold(row.EventName)}!";

                                            if (row.ProposalFileContent is not null)
                                            {
                                                string fileTitle = null;

                                                byte[] fileContent = null;

                                                MemoryStream fileStream = new MemoryStream();

                                                fileTitle = row.ProposalFileTitle;

                                                fileContent = row.ProposalFileContent;

                                                fileStream = new MemoryStream(fileContent);

                                                reminderMessageBuilder.WithFiles(new Dictionary<string, Stream>() { { fileTitle, fileStream } }, true);
                                            }

                                            reminderMessageBuilder.WithEmbed(reminderEmbed.Build());

                                            await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                            counter++;

                                            Events eventData = eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (eventData != null)
                                            {
                                                eventData.ExecutedReminderLevel = 0;

                                                await eventContext.SaveChangesAsync();
                                            }

                                            sentReminder = true;
                                        }
                                    }

                                    if (DateTime.Today == eventDateTime)
                                    {
                                        reminderEmbed.Description = $"Today is the the day for {Formatter.Bold(row.EventName)}!";

                                        if (row.ProposalFileContent is not null)
                                        {
                                            string fileTitle = null;

                                            byte[] fileContent = null;

                                            MemoryStream fileStream = new MemoryStream();

                                            fileTitle = row.ProposalFileTitle;

                                            fileContent = row.ProposalFileContent;

                                            fileStream = new MemoryStream(fileContent);

                                            reminderMessageBuilder.WithFiles(new Dictionary<string, Stream>() { { fileTitle, fileStream } }, true);
                                        }

                                        reminderMessageBuilder.WithEmbed(reminderEmbed.Build());

                                        await eventsChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                        counter++;

                                        Events eventData = eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                        if (eventData != null)
                                        {
                                            eventData.Expired = true;

                                            await eventContext.SaveChangesAsync();
                                        }

                                        sentReminder = true;
                                    }

                                    else if (DateTime.Today > eventDateTime)
                                    {
                                        Events eventData = eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                        if (eventData != null)
                                        {
                                            eventData.Expired = true;

                                            await eventContext.SaveChangesAsync();
                                        }

                                        _logger.LogInformation("Marked '{EventName}' (ID: {Id}) as expired.", row.EventName, row.Id);
                                    }

                                    processingStopWatch.Stop();

                                    if (sentReminder)
                                    {
                                        sentReminder = false;

                                        _logger.LogInformation("Sent event reminder for '{EventName}' (ID: {Id}) in {ElapsedMilliseconds} ms.", row.EventName, row.Id, processingStopWatch.ElapsedMilliseconds);
                                    }

                                    processingStopWatch.Reset();

                                    reminderEmbed.ClearFields();
                                }

                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "An error occured while processing an event (ID: {id}).", row.Id);
                                }
                            }
                        }

                        stopwatch.Stop();
                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        _logger.LogInformation("Reminded {Counter} ({CountWords}) events in {ElapsedMilliseconds} ms.", counter, counter.ToWords(), elapsedMilliseconds);

                        stopwatch.Reset();
                        await Task.Delay(TimeSpan.FromMinutes(1).Subtract(TimeSpan.FromMilliseconds(elapsedMilliseconds)));
                    }
                }

                catch (Exception ex)
                {
                    var exception = ex;
                    while (exception is AggregateException)
                        exception = exception.InnerException;

                    _logger.LogCritical("Events reminder task threw an exception: {ExceptionType}: {ExceptionMessage}.", exception.GetType(), exception.Message);

                    await errorLogsChannel.SendMessageAsync($"{ex.Message}");
                }
            });

            initialized = true;

            _logger.LogInformation("Initialized events reminder task.");
        }
    }
}
