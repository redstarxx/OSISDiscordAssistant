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

        public EventReminderService(IServiceScopeFactory serviceScopeFactory, ILogger<EventReminderService> logger, DiscordShardedClient shardedClient)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
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

                            foreach (var row in eventContext.Events)
                            {
                                try
                                {
                                    if (row.Expired is false)
                                    {
                                        processingStopWatch.Start();

                                        DateTime eventDateTime = ClientUtilities.ConvertUnixTimestampToDateTime(row.EventDateUnixTimestamp);

                                        DateTime nextEventReminderDateTime = ClientUtilities.ConvertUnixTimestampToDateTime(row.NextScheduledReminderUnixTimestamp);

                                        TimeSpan timeSpan = eventDateTime - DateTime.Today;

                                        reminderEmbed.Title = $"Events Manager - Reminding {row.EventName}... (ID: {row.Id})";

                                        reminderEmbed.AddField("Ketua / Wakil Ketua Event", row.PersonInCharge, true);
                                        reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", Formatter.Timestamp(eventDateTime, TimestampFormat.LongDate), true);
                                        reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                        reminderMessageBuilder.WithContent("@everyone");

                                        if (DateTime.Today == nextEventReminderDateTime || (DateTime.Today > nextEventReminderDateTime && DateTime.Today != eventDateTime))
                                        {
                                            if (row.ExecutedReminderLevel is 1 or 2 or 3)
                                            {
                                                reminderEmbed.Description = $"In {timeSpan.Days} days, it will be the day for {Formatter.Bold(row.EventName)}!";

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

                                                switch (eventData.ExecutedReminderLevel)
                                                {
                                                    // Level 2
                                                    case 1:
                                                        eventData.NextScheduledReminderUnixTimestamp = ClientUtilities.ConvertDateTimeToUnixTimestamp(eventDateTime.Subtract(TimeSpan.FromDays(14)));
                                                        break;
                                                    // Level 3
                                                    case 2:
                                                        eventData.NextScheduledReminderUnixTimestamp = ClientUtilities.ConvertDateTimeToUnixTimestamp(eventDateTime.Subtract(TimeSpan.FromDays(7)));
                                                        break;
                                                    // Level 4
                                                    case 3:
                                                        eventData.NextScheduledReminderUnixTimestamp = ClientUtilities.ConvertDateTimeToUnixTimestamp(eventDateTime.Subtract(TimeSpan.FromDays(1)));
                                                        break;
                                                    default:
                                                        break;
                                                }

                                                if (eventData != null)
                                                {
                                                    eventData.ExecutedReminderLevel = row.ExecutedReminderLevel + 1;
                                                }

                                                await eventContext.SaveChangesAsync();

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
                                                }

                                                await eventContext.SaveChangesAsync();

                                                sentReminder = true;
                                            }
                                        }

                                        else if (DateTime.Today == eventDateTime)
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
                                            }

                                            await eventContext.SaveChangesAsync();

                                            sentReminder = true;
                                        }

                                        if (DateTime.Today > eventDateTime)
                                        {
                                            Events eventData = eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (eventData != null)
                                            {
                                                eventData.Expired = true;
                                            }

                                            await eventContext.SaveChangesAsync();

                                            _logger.LogInformation($"Marked '{row.EventName}' (ID: {row.Id}) as expired.", DateTime.Now);
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
                                }

                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"An error occured while processing an event (ID: {row.Id}).");
                                }
                            }
                        }

                        stopwatch.Stop();
                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        _logger.LogInformation($"Reminded {counter} ({counter.ToWords()}) events in in {elapsedMilliseconds} ms.", DateTime.Now);

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
