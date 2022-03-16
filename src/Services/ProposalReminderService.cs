using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
    public class ProposalReminderService : IProposalReminderService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ProposalReminderService> _logger;
        private readonly DiscordShardedClient _shardedClient;

        private bool initialized = false;

        public ProposalReminderService(IServiceScopeFactory serviceScopeFactory, ILogger<ProposalReminderService> logger, EventContext eventContext, DiscordShardedClient shardedClient)
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
                DiscordChannel proposalChannel = await _shardedClient.GetShard(SharedData.MainGuildId).GetChannelAsync(SharedData.ProposalChannelId);

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

                                    TimeSpan timeSpan = eventDateTime - DateTime.Today;

                                    if (timeSpan.Days == 30 || timeSpan.Days > 6 && timeSpan.Days < 30)
                                    {
                                        if (row.ProposalReminded == false)
                                        {
                                            reminderEmbed.Title = $"Events Manager - Proposal Submission for {row.EventName}... (ID: {row.Id})";
                                            reminderEmbed.Description = $"Make sure you have submitted your respective proposals in preparation for {Formatter.Bold(row.EventName)}!";

                                            reminderEmbed.AddField("Tanggal / Waktu Pelaksanaan", Formatter.Timestamp(eventDateTime, TimestampFormat.LongDate), true);
                                            reminderEmbed.AddField("Informasi Tambahan", row.EventDescription, true);

                                            reminderMessageBuilder.WithContent(row.PersonInCharge)
                                                                  .WithEmbed(embed: reminderEmbed);

                                            await proposalChannel.SendMessageAsync(builder: reminderMessageBuilder);
                                            counter++;

                                            Events eventData = eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (eventData != null)
                                            {
                                                eventData.ProposalReminded = true;
                                            }

                                            await eventContext.SaveChangesAsync();

                                            sentReminder = true;
                                        }
                                    }

                                    if (DateTime.Today == eventDateTime)
                                    {
                                        if (row.ProposalReminded == false)
                                        {
                                            Events eventData = eventContext.Events.SingleOrDefault(x => x.Id == row.Id);

                                            if (eventData != null)
                                            {
                                                eventData.ProposalReminded = true;
                                            }

                                            await eventContext.SaveChangesAsync();
                                        }
                                    }

                                    processingStopWatch.Stop();

                                    if (sentReminder)
                                    {
                                        sentReminder = false;

                                        _logger.LogInformation("Sent proposal reminder for '{EventName}' (ID: {Id}) in {ElapsedMilliseconds} ms.", row.EventName, row.Id, processingStopWatch.ElapsedMilliseconds);
                                    }

                                    processingStopWatch.Reset();
                                }

                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "An error occured while processing an event (ID: {Id}).", row.Id);
                                }
                            }
                        }                        

                        stopwatch.Stop();
                        long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        _logger.LogInformation("Reminded {Counter} ({CountWords}) proposal submissions in {ElapsedMilliseconds} ms.", counter, counter.ToWords(), elapsedMilliseconds);

                        stopwatch.Reset();
                        await Task.Delay(TimeSpan.FromMinutes(1).Subtract(TimeSpan.FromMilliseconds(elapsedMilliseconds)));
                    }
                }

                catch (Exception ex)
                {
                    var exception = ex;
                    while (exception is AggregateException)
                        exception = exception.InnerException;

                    _logger.LogCritical("Proposal submission reminder task threw an exception: {ExceptionType}: {ExceptionMessage}.", exception.GetType(), exception.Message);

                    await errorLogsChannel.SendMessageAsync($"{ex.Message}");
                }
            });

            initialized = true;

            _logger.LogInformation("Initialized proposal submission reminder task.");
        }
    }
}
